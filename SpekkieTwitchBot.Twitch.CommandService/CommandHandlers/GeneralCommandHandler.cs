using SpekkieTwitchBot.General.FileHandling.General;
using TwitchLib.Client.Models;

namespace CommandService.CommandHandlers;

public class GeneralCommandHandler(
    GeneralFileReader generalFileReader,
    GeneralFileWriter generalFileWriter,
    SpotifyCommandHandler spotifyCommandHandler,
    ObsCommandHandler obsCommandHandler,
    TimerCommandHandler timerCommandHandler,
    TwitchCommandHandler twitchCommandHandler)
{
    private Dictionary<string, Func<string>> _CommandHandlers = new();

    public string HandleCommand(ChatCommand command)
    {
        string? username = command.ChatMessage.DisplayName;
        string? commandText = command.CommandText;
        string? commandArgs = command.ArgumentsAsString;

        _CommandHandlers = new Dictionary<string, Func<string>>
        {
            { "commands", HandleCommandsCommand },
            { "afgeleid", HandleAfgeleidCommand },
            { "refund", () => twitchCommandHandler.HandleRefundCommand(commandArgs) },
            { "complete", () => twitchCommandHandler.HandleCompleteCommand(commandArgs) },

            { "hello", TextCommandHandler.HandleHelloCommand },
            { "twitter", TextCommandHandler.HandleGetTwitterCommand },
            { "youtube", TextCommandHandler.HandleGetYouTubeCommand },
            { "discord", TextCommandHandler.HandleGetDiscordCommand },
            { "lurk", () => TextCommandHandler.HandleLurkCommand(username) },
            { "tag", TextCommandHandler.HandleGetCocTagCommand },

            { "song", spotifyCommandHandler.HandleGetCurrentSongCommand },
            { "playlist", spotifyCommandHandler.HandleGetCurrentPlaylistCommand },
            { "pausemusic", spotifyCommandHandler.HandlePauseMusicCommand },
            { "resumemusic", spotifyCommandHandler.HandleResumeMusicCommand },
            { "next", spotifyCommandHandler.HandleNextSongCommand },
            { "prev", spotifyCommandHandler.HandlePrevSongCommand },
            { "queue", spotifyCommandHandler.HandleGetQueueCommand },
            { "addsong", () => spotifyCommandHandler.HandleAddSongToQueueCommand(commandArgs) },
            { "playsong", () => spotifyCommandHandler.HandlePlaySpecificSongCommand(commandArgs, username) },
            { "createredemption", () => twitchCommandHandler.HandleCreateRedemptionCommand(commandArgs) },

            { "setscene", () => obsCommandHandler.HandleSetSceneCommand(commandArgs) },
            { "mutemic", () => obsCommandHandler.HandleSetInputMute("microphone") },
            { "mutemusic", () => obsCommandHandler.HandleSetInputMute("spotify") },
            { "standardvolumes", obsCommandHandler.HandleSetStandardVolumes },
            { "volumezero", obsCommandHandler.HandleVolumeZero },
            
            { "pausetimer", timerCommandHandler.HandlePauseTimerCommand },
            { "starttimer", timerCommandHandler.HandleStartTimerCommand },
            { "addtime", () => timerCommandHandler.HandleAddTimeToTimerCommand(commandArgs) },
            { "settime", () => timerCommandHandler.HandleSetTimeOnTimerCommand(commandArgs) }
        };

        return _CommandHandlers.TryGetValue(commandText, out Func<string>? handler) ? handler.Invoke() : HandleUnknownCommand();
    }

    private string HandleCommandsCommand()
    {
        string commands = "";
        foreach (string command in _CommandHandlers.Keys)
            if (command != _CommandHandlers.Keys.Last())
                commands += $"{command}, ";
            else
                commands += $"{command}";
        return $"The following commands are available on this channel: {commands}";
    }

    private static string HandleUnknownCommand()
    {
        return "Unknown command";
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