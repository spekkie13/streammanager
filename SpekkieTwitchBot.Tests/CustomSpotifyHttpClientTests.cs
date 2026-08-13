using Moq;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieTwitchBot.General.FileHandling;
using SpotifyAuthService.Auth;
using SpotifyAuthService.General;
using AuthService = SpotifyAuthService.Auth.SpotifyAuthService;

namespace SpekkieTwitchBot.Tests;

// Covers the retry gate: a revoked refresh token must stop producing token requests entirely,
// while ordinary failures keep the existing timed backoff.
public class CustomSpotifyHttpClientTests
{
    private readonly Mock<AuthService> _Auth = new(MockBehavior.Loose, null!, null!, null!, null!);
    private readonly Mock<Logger> _Logger = new(MockBehavior.Loose, null!);

    private DateTime _AuthFileStamp = new(2026, 8, 13, 12, 0, 0, DateTimeKind.Utc);

    public CustomSpotifyHttpClientTests()
    {
        _Auth.Setup(a => a.GetSpotifyAuth()).Returns(new SpotifyAuth
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RefreshToken = "refresh"
        });

        _Auth.Setup(a => a.GetAuthFileStampUtc()).Returns(() => _AuthFileStamp);
    }

    // No request is ever issued: EnsureConfigured throws before the HttpClient is touched.
    private CustomSpotifyHttpClient CreateClient() =>
        new(new HttpClient(), _Auth.Object, _Logger.Object);

    private void FailAuthWith(SpotifyAuthException ex) =>
        _Auth.Setup(a => a.FixAuth(It.IsAny<SpotifyAuth>())).ThrowsAsync(ex);

    private static SpotifyAuthException Revoked() =>
        new("token refresh failed: 400 — invalid_grant", true);

    private static SpotifyAuthException Transient() =>
        new("token refresh failed: 500 — upstream boom");

    [Fact]
    public async Task RevokedToken_DoesNotAttemptAuthAgainWhileAuthFileIsUnchanged()
    {
        FailAuthWith(Revoked());
        CustomSpotifyHttpClient client = CreateClient();

        for (int i = 0; i < 3; i++)
            await Assert.ThrowsAsync<SpotifyAuthException>(() => client.PutAsync("https://example.invalid", null));

        // The whole point of the fix: one attempt, then silence until a human intervenes.
        _Auth.Verify(a => a.FixAuth(It.IsAny<SpotifyAuth>()), Times.Once);
    }

    [Fact]
    public async Task RevokedToken_RethrowsFlaggedExceptionSoTheHostedServiceCanRecogniseIt()
    {
        FailAuthWith(Revoked());
        CustomSpotifyHttpClient client = CreateClient();

        await Assert.ThrowsAsync<SpotifyAuthException>(() => client.PutAsync("https://example.invalid", null));

        // Second call is served from the gate, not the catch block - it must still carry the flag,
        // otherwise SpotifyHostedService would fall through to its generic "retrying" branch.
        SpotifyAuthException ex = await Assert.ThrowsAsync<SpotifyAuthException>(
            () => client.PutAsync("https://example.invalid", null));

        Assert.True(ex.RequiresReauthorization);
    }

    [Fact]
    public async Task RevokedToken_LogsTheActionableMessageOnlyOnce()
    {
        FailAuthWith(Revoked());
        CustomSpotifyHttpClient client = CreateClient();

        for (int i = 0; i < 3; i++)
            await Assert.ThrowsAsync<SpotifyAuthException>(() => client.PutAsync("https://example.invalid", null));

        _Logger.Verify(
            l => l.LogError(It.Is<string>(m => m.Contains("Re-authorization required"))),
            Times.Once);
    }

    [Fact]
    public async Task RevokedToken_RetriesOnceTheAuthFileChanges()
    {
        FailAuthWith(Revoked());
        CustomSpotifyHttpClient client = CreateClient();

        await Assert.ThrowsAsync<SpotifyAuthException>(() => client.PutAsync("https://example.invalid", null));
        await Assert.ThrowsAsync<SpotifyAuthException>(() => client.PutAsync("https://example.invalid", null));
        _Auth.Verify(a => a.FixAuth(It.IsAny<SpotifyAuth>()), Times.Once);

        // Tools/Reauth-Spotify.ps1 rewriting Spotify.json is what this simulates.
        _AuthFileStamp = _AuthFileStamp.AddMinutes(1);

        await Assert.ThrowsAsync<SpotifyAuthException>(() => client.PutAsync("https://example.invalid", null));
        _Auth.Verify(a => a.FixAuth(It.IsAny<SpotifyAuth>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TransientFailure_UsesTimedBackoffAndNeverConsultsTheAuthFile()
    {
        FailAuthWith(Transient());
        CustomSpotifyHttpClient client = CreateClient();

        for (int i = 0; i < 3; i++)
            await Assert.ThrowsAsync<SpotifyAuthException>(() => client.PutAsync("https://example.invalid", null));

        // Held off by the 5-minute cooldown, not the re-auth gate.
        _Auth.Verify(a => a.FixAuth(It.IsAny<SpotifyAuth>()), Times.Once);
        _Auth.Verify(a => a.GetAuthFileStampUtc(), Times.Never);

        _Logger.Verify(l => l.LogError(It.Is<string>(m => m.Contains("retrying in"))), Times.Once);
        _Logger.Verify(
            l => l.LogError(It.Is<string>(m => m.Contains("Re-authorization required"))),
            Times.Never);
    }
}
