using System.Net;
using System.Text;
using Moq;
using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Tests;

public class FileBackedTwitchAuthTokenProviderTests
{
    private readonly Mock<ITwitchFileReader> _Reader = new();
    private readonly Mock<ITwitchFileWriter> _Writer = new();
    private readonly Mock<Logger> _Logger = new(MockBehavior.Loose, null!);
    
    private FileBackedTwitchAuthTokenProvider CreateProvider(HttpMessageHandler handler)
    {
        HttpClient httpClient = new(handler);
        return new FileBackedTwitchAuthTokenProvider(_Reader.Object, _Writer.Object, httpClient, _Logger.Object);
    }

    private static string UserFileJson(TwitchUserFile file) => JsonConvert.SerializeObject(file);

    private static HttpMessageHandler OkHandler(object responseBody)
    {
        string json = JsonConvert.SerializeObject(responseBody);
        return new FakeHttpHandler(HttpStatusCode.OK, json);
    }

    private static HttpMessageHandler StatusHandler(HttpStatusCode code)
        => new FakeHttpHandler(code, "{}");

    // ── GetUserAccessTokenAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetUserAccessToken_WithRefreshToken_CallsRefreshAndReturnsNewToken()
    {
        TwitchUserFile userFile = new() { ClientId = "cid", ClientSecret = "cs", UserRefreshToken = "rtoken" };
        _Reader.Setup(r => r.ReadTwitchUserAuthFile()).Returns(UserFileJson(userFile));

        AuthorizationCredentials creds = new() { AccessToken = "new-access", RefreshToken = "new-refresh" };
        FileBackedTwitchAuthTokenProvider provider = CreateProvider(OkHandler(creds));

        string token = await provider.GetUserAccessTokenAsync(CancellationToken.None);

        Assert.Equal("new-access", token);
    }

    [Fact]
    public async Task GetUserAccessToken_WithRefreshToken_PersistsUpdatedTokenToFile()
    {
        TwitchUserFile userFile = new() { ClientId = "cid", ClientSecret = "cs", UserRefreshToken = "rtoken" };
        _Reader.Setup(r => r.ReadTwitchUserAuthFile()).Returns(UserFileJson(userFile));

        AuthorizationCredentials creds = new() { AccessToken = "new-access", RefreshToken = "new-refresh" };
        FileBackedTwitchAuthTokenProvider provider = CreateProvider(OkHandler(creds));

        await provider.GetUserAccessTokenAsync(CancellationToken.None);

        _Writer.Verify(w => w.WriteTwitchUserAuthFile(It.Is<string>(s => s.Contains("new-access"))));
    }

    [Fact]
    public async Task GetUserAccessToken_WithCode_ExchangesCodeForToken()
    {
        TwitchUserFile userFile = new() { ClientId = "cid", ClientSecret = "cs", Code = "auth-code" };
        _Reader.Setup(r => r.ReadTwitchUserAuthFile()).Returns(UserFileJson(userFile));

        AuthorizationCredentials creds = new() { AccessToken = "exchanged-token", RefreshToken = "new-refresh" };
        FileBackedTwitchAuthTokenProvider provider = CreateProvider(OkHandler(creds));

        string token = await provider.GetUserAccessTokenAsync(CancellationToken.None);

        Assert.Equal("exchanged-token", token);
    }

    [Fact]
    public async Task GetUserAccessToken_CodeExchangeBadRequest_NoRefreshToken_LogsErrorAndReturnsEmpty()
    {
        // Code only (no refresh token) — BadRequest means we're stuck, logs error and returns empty
        TwitchUserFile userFile = new() { ClientId = "cid", ClientSecret = "cs", Code = "stale-code" };
        _Reader.Setup(r => r.ReadTwitchUserAuthFile()).Returns(UserFileJson(userFile));

        FileBackedTwitchAuthTokenProvider provider = CreateProvider(StatusHandler(HttpStatusCode.BadRequest));

        string token = await provider.GetUserAccessTokenAsync(CancellationToken.None);

        Assert.Equal("", token);
        _Logger.Verify(l => l.LogError(It.IsAny<string>()));
    }

    [Fact]
    public async Task GetUserAccessToken_NoTokenOrCode_LogsErrorAndReturnsEmpty()
    {
        TwitchUserFile userFile = new() { ClientId = "cid", ClientSecret = "cs" };
        _Reader.Setup(r => r.ReadTwitchUserAuthFile()).Returns(UserFileJson(userFile));

        FileBackedTwitchAuthTokenProvider provider = CreateProvider(StatusHandler(HttpStatusCode.OK));

        string token = await provider.GetUserAccessTokenAsync(CancellationToken.None);

        Assert.Equal("", token);
        _Logger.Verify(l => l.LogError(It.IsAny<string>()));
    }

    // ── ForceRefreshAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ForceRefresh_NoRefreshToken_Throws()
    {
        TwitchUserFile userFile = new() { ClientId = "cid", ClientSecret = "cs" };
        _Reader.Setup(r => r.ReadTwitchUserAuthFile()).Returns(UserFileJson(userFile));

        FileBackedTwitchAuthTokenProvider provider = CreateProvider(StatusHandler(HttpStatusCode.OK));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.ForceRefreshAsync(CancellationToken.None));
    }

    // ── ReadIdentityAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ReadIdentity_CachesAfterFirstRead()
    {
        TwitchGeneralFile generalFile = new() { BotName = "spekkie", BroadcasterName = "tomspek" };
        _Reader.Setup(r => r.ReadTwitchGeneralAuthFile()).Returns(JsonConvert.SerializeObject(generalFile));

        FileBackedTwitchAuthTokenProvider provider = CreateProvider(StatusHandler(HttpStatusCode.OK));

        TwitchGeneralFile first = await provider.ReadIdentityAsync(CancellationToken.None);
        TwitchGeneralFile second = await provider.ReadIdentityAsync(CancellationToken.None);

        // File should only be read once due to caching
        _Reader.Verify(r => r.ReadTwitchGeneralAuthFile(), Times.Once);
        Assert.Same(first, second);
    }
}

// ── HTTP handler helpers ─────────────────────────────────────────────────────

file sealed class FakeHttpHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
}