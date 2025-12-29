using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch;

public class FileTwitchTokenStore : ITwitchTokenStore
{
    private readonly TwitchFileReader _Reader;
    private readonly TwitchFileWriter _Writer;

    public FileTwitchTokenStore(TwitchFileReader reader, TwitchFileWriter writer)
    {
        _Reader = reader;
        _Writer = writer;
    }

    public Task<TwitchUserFile> LoadUserAsync(CancellationToken ct)
    {
        var json = _Reader.ReadTwitchUserAuthFile();
        var legacy = JsonConvert.DeserializeObject<TwitchUserFile>(json) ?? new();

        return Task.FromResult(new TwitchUserFile
        {
            ClientId = legacy.ClientId ?? "",
            ClientSecret = legacy.ClientSecret ?? "",
            UserToken = legacy.UserToken ?? "",
            UserRefreshToken = legacy.UserRefreshToken ?? "",
            Code = legacy.Code ?? "",
        });
    }
    
    public Task SaveUserAsync(TwitchUserFile userFile, CancellationToken ct)
    {
        _Writer.WriteTwitchUserAuthFile(JsonConvert.SerializeObject(userFile));
        return Task.CompletedTask;
    }

    public Task<TwitchGeneralFile> LoadGeneralSettingsAsync(CancellationToken ct)
    {
        var json = _Reader.ReadTwitchGeneralAuthFile();
        var legacy = JsonConvert.DeserializeObject<TwitchGeneralFile>(json) ?? new();

        return Task.FromResult(new TwitchGeneralFile
        {
            BotName = legacy.BotName ?? "",
            BroadcasterName = legacy.BroadcasterName ?? "",
            ChannelId = legacy.ChannelId ?? "",
            Obs_Url = legacy.Obs_Url ?? "",
            Password = legacy.Password ?? "",
            ImplicitOAuth = legacy.ImplicitOAuth ?? "",
        });
    }

    public Task SaveGeneralSettingsAsync(TwitchGeneralFile general, CancellationToken ct)
    {
        _Writer.WriteTwitchGeneralAuthFile(JsonConvert.SerializeObject(general));
        return Task.CompletedTask;
    }
}