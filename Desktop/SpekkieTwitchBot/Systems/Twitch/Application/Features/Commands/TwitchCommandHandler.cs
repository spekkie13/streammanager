using SpekkieTwitchBot.Systems.Twitch.Abstractions;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TwitchCommandHandler : ITwitchCommandHandler
{
    private readonly ChannelPointsFeature _channelPointsFeature;
    private readonly ITwitchChannelInfoClient _api;

    public TwitchCommandHandler(ChannelPointsFeature channelPointsFeature, ITwitchChannelInfoClient api)
    {
        _channelPointsFeature = channelPointsFeature;
        _api = api;
    }

    public async Task<string> HandleCreateRedemptionCommand(string commandArgs)
        => await _channelPointsFeature.CreateRedemption(commandArgs);

    public async Task<string> HandleUptimeCommand(CancellationToken ct)
    {
        DateTimeOffset? startTime = await _api.GetStreamStartTimeAsync(ct);
        if (startTime == null)
            return "The stream is currently offline. | De stream is momenteel offline.";

        TimeSpan uptime = DateTimeOffset.UtcNow - startTime.Value;
        int hours = (int)uptime.TotalHours;
        int minutes = uptime.Minutes;

        string en = hours > 0
            ? $"Stream has been live for {hours}h {minutes}m"
            : $"Stream has been live for {minutes}m";
        string nl = hours > 0
            ? $"Stream is al {hours}u {minutes}m live"
            : $"Stream is al {minutes}m live";

        return $"{en} | {nl}";
    }

    public async Task<string> HandleClipCommand(CancellationToken ct)
    {
        string? clipUrl = await _api.CreateClipAsync(ct);
        return clipUrl != null
            ? $"Clip created! {clipUrl}"
            : "Failed to create clip — is the stream live?";
    }

    public async Task<string> HandleShoutoutCommand(string username, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username))
            return "Usage: !so <username>";

        (string? lastGame, string? login) = await _api.GetShoutoutInfoAsync(username.TrimStart('@'), ct);
        if (login == null)
            return $"Could not find user '{username}'.";

        string gameEn = string.IsNullOrWhiteSpace(lastGame) ? "something awesome" : lastGame;
        string gameNl = string.IsNullOrWhiteSpace(lastGame) ? "iets tofs" : lastGame;

        return $"Go check out @{login} — last seen playing {gameEn}! twitch.tv/{login} " +
               $"| Ga eens kijken bij @{login} — laatst gezien met {gameNl}! twitch.tv/{login}";
    }
}