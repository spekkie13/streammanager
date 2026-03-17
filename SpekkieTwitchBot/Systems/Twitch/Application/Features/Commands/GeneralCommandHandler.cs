using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class GeneralCommandHandler : IGeneralCommandHandler
{
    private readonly GeneralFileReader _GeneralFileReader;
    private readonly GeneralFileWriter _GeneralFileWriter;
    private readonly ITextCommandHandler _TextCommandHandler;
    private readonly ISpotifyCommandHandler _SpotifyCommandHandler;
    private readonly IObsCommandHandler _ObsCommandHandler;
    private readonly ITimerCommandHandler _TimerCommandHandler;
    private readonly ITwitchCommandHandler _TwitchCommandHandler;
    private readonly IClashCommandHandler _ClashCommandHandler;

    public GeneralCommandHandler(
        GeneralFileReader generalFileReader,
        GeneralFileWriter generalFileWriter,
        ITextCommandHandler textCommandHandler,
        ISpotifyCommandHandler spotifyCommandHandler,
        IObsCommandHandler obsCommandHandler,
        ITimerCommandHandler timerCommandHandler,
        ITwitchCommandHandler twitchCommandHandler,
        IClashCommandHandler clashCommandHandler
    )
    {
        _GeneralFileReader = generalFileReader;
        _GeneralFileWriter = generalFileWriter;
        _TextCommandHandler = textCommandHandler;
        _SpotifyCommandHandler = spotifyCommandHandler;
        _ObsCommandHandler = obsCommandHandler;
        _TimerCommandHandler = timerCommandHandler;
        _TwitchCommandHandler = twitchCommandHandler;
        _ClashCommandHandler = clashCommandHandler;
    }
    
    private Dictionary<string, Func<CancellationToken, Task<string>>> _CommandHandlers = new();

    public async Task<string> HandleCommand(ChatCommandReceived command, CancellationToken ct)
    {
        string username = command.Username;
        string commandText = $"!{command.CommandText}";
        string commandArgs = command.ArgumentsAsString ?? "";

        _CommandHandlers = new Dictionary<string, Func<CancellationToken, Task<string>>>(StringComparer.OrdinalIgnoreCase)
        {
            // sync handlers -> wrap
            ["!commands"] = _ => Task.FromResult(HandleCommandsCommand()),
            ["!afgeleid"] = _ => Task.FromResult(HandleAfgeleidCommand()),

            // Spotify (async)
            ["!song"]        = _SpotifyCommandHandler.HandleGetCurrentSongCommand,
            ["!playlist"]    = _SpotifyCommandHandler.HandleGetCurrentPlaylistCommand,
            ["!pausemusic"]  = _SpotifyCommandHandler.HandlePauseMusicCommand,
            ["!resumemusic"] = _SpotifyCommandHandler.HandleResumeMusicCommand,
            ["!next"]       = _SpotifyCommandHandler.HandleNextSongCommand,
            ["!prev"]       = _SpotifyCommandHandler.HandlePrevSongCommand,
            ["!queue"]      = _SpotifyCommandHandler.HandleGetQueueCommand,
            ["!addsong"]    = t => _SpotifyCommandHandler.HandleAddSongToQueueCommand(commandArgs, t),
            ["!playsong"]   = t => _SpotifyCommandHandler.HandlePlaySpecificSongCommand(commandArgs, username, t),

            // Twitch (als deze sync zijn -> Task.FromResult)
            ["!createredemption"] = _ => _TwitchCommandHandler.HandleCreateRedemptionCommand(commandArgs),

            // OBS (idem)
            ["!setscene"]        = _ => Task.FromResult(_ObsCommandHandler.HandleSetSceneCommand(commandArgs)),
            ["!mutemic"]         = _ => Task.FromResult(_ObsCommandHandler.HandleSetInputMute("microphone")),
            ["!mutemusic"]       = _ => Task.FromResult(_ObsCommandHandler.HandleSetInputMute("spotify")),
            ["!standardvolumes"] = _ => Task.FromResult(_ObsCommandHandler.HandleSetStandardVolumes()),
            ["!volumezero"]      = _ => Task.FromResult(_ObsCommandHandler.HandleVolumeZero()),

            // Timer (maak async als hij async is geworden, anders Task.FromResult)
            ["!pausetimer"] = _ => Task.FromResult(_TimerCommandHandler.HandlePauseTimerCommand()),
            ["!starttimer"] = _ => Task.FromResult(_TimerCommandHandler.HandleStartTimerCommand()),
            ["!addtime"]    = _ => Task.FromResult(_TimerCommandHandler.HandleAddTimeToTimerCommand(commandArgs)),
            ["!settime"]    = _ => Task.FromResult(_TimerCommandHandler.HandleSetTimeOnTimerCommand(commandArgs)),

            // Clash of Clans
            ["!war"]          = _ => Task.FromResult(_ClashCommandHandler.HandleSetWarStatsCommand(commandArgs)),
            ["!setplayertag"] = _ => Task.FromResult(_ClashCommandHandler.HandleAddPlayerTagCommand(commandArgs)),
        };

        if (!_CommandHandlers.TryGetValue(commandText, out var handler))
            return "Unknown command";

        return await handler(ct).ConfigureAwait(false);
    }
    
    private string HandleCommandsCommand()
    {
        string commands = "";
        
        List<string> textCommands = _TextCommandHandler.GetTextCommands().Select(x => x.Command ?? "").ToList();
        textCommands.AddRange(_CommandHandlers.Keys);
        
        foreach (string command in textCommands)
            if (command != _CommandHandlers.Keys.Last())
                commands += $"{command}, ";
            else
                commands += $"{command}";
        return $"The following commands are available on this channel: {commands}";
    }

    private string HandleAfgeleidCommand()
    {
        string afgeleidtext = _GeneralFileReader.ReadAfgeleidCounter();
        int afgeleid = Convert.ToInt32(afgeleidtext);
        afgeleid++;
        _GeneralFileWriter.WriteAfgeleidCounter(afgeleid.ToString());
        return $"Spekkie is {afgeleid}x afgeleid geweest";
    }
}