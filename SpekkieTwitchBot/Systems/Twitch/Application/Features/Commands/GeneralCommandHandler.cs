using CommandService.CommandHandlers;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch;

public class GeneralCommandHandler(
    GeneralFileReader generalFileReader,
    GeneralFileWriter generalFileWriter,
    TextCommandHandler textCommandHandler,
    SpotifyCommandHandler spotifyCommandHandler,
    ObsCommandHandler obsCommandHandler,
    TimerCommandHandler timerCommandHandler,
    TwitchCommandHandler twitchCommandHandler)
{
    private Dictionary<string, Func<string>> _CommandHandlers = new();

    public string HandleCommand(ChatCommandReceived command)
    {
        string username = command.Username;
        string commandText = $"!{command.CommandText}";
        string commandArgs = command.ArgumentsAsString;

        _CommandHandlers = new Dictionary<string, Func<string>>
        { 
            { "!commands", HandleCommandsCommand },
            { "!afgeleid", HandleAfgeleidCommand },
            
            { "!song", spotifyCommandHandler.HandleGetCurrentSongCommand },
            { "!playlist", spotifyCommandHandler.HandleGetCurrentPlaylistCommand },
            { "!pausemusic", spotifyCommandHandler.HandlePauseMusicCommand },
            { "!resumemusic", spotifyCommandHandler.HandleResumeMusicCommand },
            { "!next", spotifyCommandHandler.HandleNextSongCommand },
            { "!prev", spotifyCommandHandler.HandlePrevSongCommand },
            { "!queue", spotifyCommandHandler.HandleGetQueueCommand },
            { "!addsong", () => spotifyCommandHandler.HandleAddSongToQueueCommand(commandArgs) },
            { "!playsong", () => spotifyCommandHandler.HandlePlaySpecificSongCommand(commandArgs, username) },
            { "!createredemption", () => twitchCommandHandler.HandleCreateRedemptionCommand(commandArgs) },

            { "!setscene", () => obsCommandHandler.HandleSetSceneCommand(commandArgs) },
            { "!mutemic", () => obsCommandHandler.HandleSetInputMute("microphone") },
            { "!mutemusic", () => obsCommandHandler.HandleSetInputMute("spotify") },
            { "!standardvolumes", obsCommandHandler.HandleSetStandardVolumes },
            { "!volumezero", obsCommandHandler.HandleVolumeZero },
            
            { "!pausetimer", timerCommandHandler.HandlePauseTimerCommand },
            { "!starttimer", timerCommandHandler.HandleStartTimerCommand },
            { "!addtime", () => timerCommandHandler.HandleAddTimeToTimerCommand(commandArgs) },
            { "!settime", () => timerCommandHandler.HandleSetTimeOnTimerCommand(commandArgs) }
        };

        return _CommandHandlers.TryGetValue(commandText, out Func<string>? handler)
            ? handler.Invoke()
            : "Unknown command";
    }

    private string HandleCommandsCommand()
    {
        string commands = "";
        
        List<string> textCommands = textCommandHandler.GetTextCommands().Select(x => x.Command ?? "").ToList();
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
        string afgeleidtext = generalFileReader.ReadAfgeleidCounter();
        int afgeleid = Convert.ToInt32(afgeleidtext);
        afgeleid++;
        generalFileWriter.WriteAfgeleidCounter(afgeleid.ToString());
        return $"Spekkie is {afgeleid}x afgeleid geweest";
    }
}