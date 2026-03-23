using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class GeneralCommandHandler : IGeneralCommandHandler
{
    private readonly GeneralFileReader _generalFileReader;
    private readonly GeneralFileWriter _generalFileWriter;
    private readonly ITextCommandHandler _textCommandHandler;
    private readonly ISpotifyCommandHandler _spotifyCommandHandler;
    private readonly IObsCommandHandler _obsCommandHandler;
    private readonly ITimerCommandHandler _timerCommandHandler;
    private readonly ITwitchCommandHandler _twitchCommandHandler;
    private readonly IClashCommandHandler _clashCommandHandler;
    private readonly ITwitchFileReader _twitchFileReader;

    public GeneralCommandHandler(
        GeneralFileReader generalFileReader,
        GeneralFileWriter generalFileWriter,
        ITextCommandHandler textCommandHandler,
        ISpotifyCommandHandler spotifyCommandHandler,
        IObsCommandHandler obsCommandHandler,
        ITimerCommandHandler timerCommandHandler,
        ITwitchCommandHandler twitchCommandHandler,
        IClashCommandHandler clashCommandHandler,
        ITwitchFileReader twitchFileReader
    )
    {
        _generalFileReader = generalFileReader;
        _generalFileWriter = generalFileWriter;
        _textCommandHandler = textCommandHandler;
        _spotifyCommandHandler = spotifyCommandHandler;
        _obsCommandHandler = obsCommandHandler;
        _timerCommandHandler = timerCommandHandler;
        _twitchCommandHandler = twitchCommandHandler;
        _clashCommandHandler = clashCommandHandler;
        _twitchFileReader = twitchFileReader;
    }
    
    public async Task<string> HandleCommand(ChatCommandReceived command, CancellationToken ct)
    {
        string username = command.Username;
        string commandText = $"!{command.CommandText}";
        string commandArgs = command.ArgumentsAsString ?? "";

        Dictionary<string, Func<CancellationToken, Task<string>>> commandHandlers = null!;
        commandHandlers = new Dictionary<string, Func<CancellationToken, Task<string>>>(StringComparer.OrdinalIgnoreCase)
        {
            // sync handlers -> wrap
            ["!commands"] = _ => Task.FromResult(HandleCommandsCommand()),
            ["!afgeleid"] = _ => Task.FromResult(HandleAfgeleidCommand()),

            // Spotify (async)
            ["!song"]        = _spotifyCommandHandler.HandleGetCurrentSongCommand,
            ["!playlist"]    = _spotifyCommandHandler.HandleGetCurrentPlaylistCommand,
            ["!pausemusic"]  = _spotifyCommandHandler.HandlePauseMusicCommand,
            ["!resumemusic"] = _spotifyCommandHandler.HandleResumeMusicCommand,
            ["!next"]       = _spotifyCommandHandler.HandleNextSongCommand,
            ["!prev"]       = _spotifyCommandHandler.HandlePrevSongCommand,
            ["!queue"]      = _spotifyCommandHandler.HandleGetQueueCommand,
            ["!sr"]         = t => _spotifyCommandHandler.HandleSongRequestCommand(commandArgs, command.UserId, username, t),
            ["!playsong"]   = t => _spotifyCommandHandler.HandlePlaySpecificSongCommand(commandArgs, username, t),

            // Twitch
            ["!createredemption"] = _ => _twitchCommandHandler.HandleCreateRedemptionCommand(commandArgs),
            ["!uptime"]           = t => _twitchCommandHandler.HandleUptimeCommand(t),
            ["!clip"]             = t => _twitchCommandHandler.HandleClipCommand(t),
            ["!so"]               = t => _twitchCommandHandler.HandleShoutoutCommand(commandArgs, t),

            // OBS (idem)
            ["!setscene"]        = _ => Task.FromResult(_obsCommandHandler.HandleSetSceneCommand(commandArgs)),
            ["!mutemic"]         = _ => Task.FromResult(_obsCommandHandler.HandleSetInputMute("microphone")),
            ["!mutemusic"]       = _ => Task.FromResult(_obsCommandHandler.HandleSetInputMute("spotify")),
            ["!standardvolumes"] = _ => Task.FromResult(_obsCommandHandler.HandleSetStandardVolumes()),
            ["!volumezero"]      = _ => Task.FromResult(_obsCommandHandler.HandleVolumeZero()),

            // Timer (maak async als hij async is geworden, anders Task.FromResult)
            ["!pausetimer"] = _ => Task.FromResult(_timerCommandHandler.HandlePauseTimerCommand()),
            ["!starttimer"] = _ => Task.FromResult(_timerCommandHandler.HandleStartTimerCommand()),
            ["!addtime"]    = _ => Task.FromResult(_timerCommandHandler.HandleAddTimeToTimerCommand(commandArgs)),
            ["!settime"]    = _ => Task.FromResult(_timerCommandHandler.HandleSetTimeOnTimerCommand(commandArgs)),

            // Clash of Clans
            ["!war"]          = _ => Task.FromResult(_clashCommandHandler.HandleSetWarStatsCommand(commandArgs)),
            ["!setplayertag"] = _ => _clashCommandHandler.HandleAddPlayerTagCommand(commandArgs),

            // Sub goal
            ["!subgoal"] = HandleSubGoalCommand,
            ["!subdoel"] = HandleSubGoalCommand,
        };

        if (!commandHandlers.TryGetValue(commandText, out Func<CancellationToken, Task<string>>? handler))
            return "Unknown command";

        return await handler(ct).ConfigureAwait(false);

        string HandleCommandsCommand()
        {
            List<string> all = _textCommandHandler.GetTextCommands()
                .Select(x => x.Command ?? "")
                .Concat(commandHandlers.Keys)
                .ToList();
            return $"The following commands are available on this channel: {string.Join(", ", all)}";
        }
    }

    private async Task<string> HandleSubGoalCommand(CancellationToken ct)
    {
        var config = await _twitchFileReader.ReadGoalsConfigAsync();
        if (config == null) return "No sub goal configured.";
        var sub = config.SubGoal;
        int daysRemaining = Math.Max(0, sub.EndDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber);
        return $"Sub goal: {sub.CurrentAmount}/{sub.Goal} subs before {sub.EndDate:MMM d} ({daysRemaining} days left) — Reward: {sub.RewardEn} " +
               $"| Sub doel: {sub.CurrentAmount}/{sub.Goal} subs voor {sub.EndDate:d MMM} ({daysRemaining} dagen over) — Beloning: {sub.RewardNl}";
    }

    private string HandleAfgeleidCommand()
    {
        string afgeleidtext = _generalFileReader.ReadAfgeleidCounter();
        if (!int.TryParse(afgeleidtext, out int afgeleid))
            afgeleid = 0;
        afgeleid++;
        _generalFileWriter.WriteAfgeleidCounter(afgeleid.ToString());
        return $"Spekkie is {afgeleid}x afgeleid geweest";
    }
}