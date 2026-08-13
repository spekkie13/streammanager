using System.Net;
using Moq;
using Newtonsoft.Json;
using SpekkieClassLibrary.Spotify.Auth;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpotifyAuthService.Auth;
// The namespace and the class share a name, so the bare type name is ambiguous here.
using AuthService = SpotifyAuthService.Auth.SpotifyAuthService;

namespace SpekkieTwitchBot.Tests;

public class SpotifyAuthServiceTests
{
    private readonly Mock<SpotifyFileReader> _FileReader = new(MockBehavior.Loose, null!);
    private readonly Mock<SpotifyFileWriter> _FileWriter = new(MockBehavior.Loose, null!);
    private readonly Mock<Logger> _Logger = new(MockBehavior.Loose, null!);

    private static SpotifyAuth Auth(string refreshToken = "old-refresh") => new()
    {
        ClientId = "client-id",
        ClientSecret = "client-secret",
        Token = "stale-access",
        RefreshToken = refreshToken
    };

    private AuthService CreateService(HttpStatusCode status, string body) =>
        new(_FileReader.Object, _FileWriter.Object, _Logger.Object, new StubHandler(status, body));

    // ── invalid_grant detection ──────────────────────────────────────────────

    [Fact]
    public async Task FixAuth_InvalidGrant_ThrowsFlaggedForReauthorization()
    {
        AuthService service = CreateService(HttpStatusCode.BadRequest,
            """{"error":"invalid_grant","error_description":"Refresh token revoked"}""");

        SpotifyAuthException ex =
            await Assert.ThrowsAsync<SpotifyAuthException>(() => service.FixAuth(Auth()));

        Assert.True(ex.RequiresReauthorization);
    }

    [Fact]
    public async Task FixAuth_InvalidClient_IsNotFlaggedForReauthorization()
    {
        // A rotated client secret is a different failure: the refresh token itself may still be
        // fine once the file is corrected, so it must keep using the normal retry backoff.
        AuthService service = CreateService(HttpStatusCode.BadRequest,
            """{"error":"invalid_client","error_description":"Invalid client secret"}""");

        SpotifyAuthException ex =
            await Assert.ThrowsAsync<SpotifyAuthException>(() => service.FixAuth(Auth()));

        Assert.False(ex.RequiresReauthorization);
    }

    [Fact]
    public async Task FixAuth_ServerError_IsNotFlaggedForReauthorization()
    {
        AuthService service = CreateService(HttpStatusCode.InternalServerError, "upstream boom");

        SpotifyAuthException ex =
            await Assert.ThrowsAsync<SpotifyAuthException>(() => service.FixAuth(Auth()));

        Assert.False(ex.RequiresReauthorization);
    }

    [Fact]
    public async Task FixAuth_NonJsonBodyMentioningInvalidGrant_IsFlaggedForReauthorization()
    {
        // Proxy error pages and truncated responses are not parseable JSON; the substring fallback
        // must still classify them rather than retrying a dead token forever.
        AuthService service = CreateService(HttpStatusCode.BadRequest,
            "<html><body>error: invalid_grant</body></html>");

        SpotifyAuthException ex =
            await Assert.ThrowsAsync<SpotifyAuthException>(() => service.FixAuth(Auth()));

        Assert.True(ex.RequiresReauthorization);
    }

    [Fact]
    public async Task FixAuth_SuccessWithoutAccessToken_Throws()
    {
        AuthService service = CreateService(HttpStatusCode.OK, """{"refresh_token":"new-refresh"}""");

        await Assert.ThrowsAsync<SpotifyAuthException>(() => service.FixAuth(Auth()));
    }

    // ── refresh_token rotation ───────────────────────────────────────────────

    [Fact]
    public async Task FixAuth_RotatedRefreshToken_PersistsAndReturnsNewToken()
    {
        string? written = null;
        _FileWriter.Setup(w => w.WriteSpotifyAuthFile(It.IsAny<string>()))
                   .Callback<string>(json => written = json);

        AuthService service = CreateService(HttpStatusCode.OK,
            """{"access_token":"fresh-access","refresh_token":"new-refresh"}""");

        SpotifyAuth result = await service.FixAuth(Auth());

        Assert.Equal("fresh-access", result.Token);
        Assert.Equal("new-refresh", result.RefreshToken);

        _FileWriter.Verify(w => w.WriteSpotifyAuthFile(It.IsAny<string>()), Times.Once);

        Assert.NotNull(written);
        SpotifyAuth persisted = JsonConvert.DeserializeObject<SpotifyAuth>(written!)!;
        Assert.Equal("new-refresh", persisted.RefreshToken);
        Assert.Equal("client-id", persisted.ClientId);
        Assert.Equal("client-secret", persisted.ClientSecret);
    }

    [Fact]
    public async Task FixAuth_RotatedRefreshToken_DoesNotWriteNullKeys()
    {
        string? written = null;
        _FileWriter.Setup(w => w.WriteSpotifyAuthFile(It.IsAny<string>()))
                   .Callback<string>(json => written = json);

        SpotifyAuth auth = Auth();
        auth.Code = null;

        AuthService service = CreateService(HttpStatusCode.OK,
            """{"access_token":"fresh-access","refresh_token":"new-refresh"}""");

        await service.FixAuth(auth);

        Assert.NotNull(written);
        Assert.DoesNotContain("\"Code\"", written);
        Assert.DoesNotContain("null", written);
    }

    [Fact]
    public async Task FixAuth_UnchangedRefreshToken_DoesNotRewriteFile()
    {
        AuthService service = CreateService(HttpStatusCode.OK,
            """{"access_token":"fresh-access","refresh_token":"old-refresh"}""");

        SpotifyAuth result = await service.FixAuth(Auth());

        Assert.Equal("fresh-access", result.Token);
        _FileWriter.Verify(w => w.WriteSpotifyAuthFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FixAuth_ResponseOmitsRefreshToken_KeepsExistingAndDoesNotWrite()
    {
        AuthService service = CreateService(HttpStatusCode.OK, """{"access_token":"fresh-access"}""");

        SpotifyAuth result = await service.FixAuth(Auth());

        Assert.Equal("old-refresh", result.RefreshToken);
        _FileWriter.Verify(w => w.WriteSpotifyAuthFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task FixAuth_PersistFails_StillReturnsFreshTokenAndLogsError()
    {
        _FileWriter.Setup(w => w.WriteSpotifyAuthFile(It.IsAny<string>()))
                   .Throws(new IOException("file locked"));

        AuthService service = CreateService(HttpStatusCode.OK,
            """{"access_token":"fresh-access","refresh_token":"new-refresh"}""");

        SpotifyAuth result = await service.FixAuth(Auth());

        // The in-memory token still works for this session, so a failed write must not kill the tick.
        Assert.Equal("fresh-access", result.Token);
        _Logger.Verify(l => l.LogError(It.Is<string>(m => m.Contains("persist"))), Times.Once);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _Status;
        private readonly string _Body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _Status = status;
            _Body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_Status) { Content = new StringContent(_Body) });
    }
}
