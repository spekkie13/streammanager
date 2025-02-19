using SpekkieClassLibrary.Constants;
using SpekkieTwitchBot.General.FileHandling.General;
using TwitchLib.Client.Models;

namespace CommandService.CommandHandlers;

public class GeneralCommandHandler
{
    private readonly GeneralFileReader _GeneralFileReader;
    private readonly GeneralFileWriter _GeneralFileWriter;
    private readonly IrcClient _IrcClient;
    private readonly ObsCommandHandler _ObsCommandHandler;
    private readonly SpotifyCommandHandler _SpotifyCommandHandler;
    private readonly TextCommandHandler _TextCommandHandler;
    private readonly ClashCommandHandler _ClashCommandHandler;
    private readonly TwitchCommandHandler _TwitchCommandHandler;

    private Dictionary<string, Action> _CommandHandlers = new();

    public GeneralCommandHandler(
        IrcClient ircClient,
        GeneralFileReader generalFileReader,
        GeneralFileWriter generalFileWriter,
        TextCommandHandler textCommandHandler,
        SpotifyCommandHandler spotifyCommandHandler,
        ObsCommandHandler obsCommandHandler,
        ClashCommandHandler clashCommandHandler,
        TwitchCommandHandler twitchCommandHandler)
    {
        _IrcClient = ircClient;
        _GeneralFileReader = generalFileReader;
        _GeneralFileWriter = generalFileWriter;
        _TextCommandHandler = textCommandHandler;
        _SpotifyCommandHandler = spotifyCommandHandler;
        _ObsCommandHandler = obsCommandHandler;
        _ClashCommandHandler = clashCommandHandler;
        _TwitchCommandHandler = twitchCommandHandler;
    }

    public void HandleCommand(ChatCommand command)
    {
        var username = command.ChatMessage.DisplayName;
        var commandText = command.CommandText;
        var commandArgs = command.ArgumentsAsString;

        _CommandHandlers = new Dictionary<string, Action>
        {
            { "commands", HandleCommandsCommand },
            { "exitbot", () => HandleExitBotCommand(username) },
            { "afgeleid", HandleAfgeleidCommand },
            { "refund", () => _TwitchCommandHandler.HandleRefundCommand(commandArgs) },
            { "complete", () => _TwitchCommandHandler.HandleCompleteCommand(commandArgs) },
            { "specs", _TextCommandHandler.HandleSpecsCommand },

            { "hello", _TextCommandHandler.HandleHelloCommand },
            { "twitter", _TextCommandHandler.HandleGetTwitterCommand },
            { "youtube", _TextCommandHandler.HandleGetYouTubeCommand },
            { "discord", _TextCommandHandler.HandleGetDiscordCommand },
            { "lurk", () => _TextCommandHandler.HandleLurkCommand(username) },
            { "tag", _TextCommandHandler.HandleGetCocTagCommand },

            { "song", _SpotifyCommandHandler.HandleGetCurrentSongCommand },
            { "playlist", _SpotifyCommandHandler.HandleGetCurrentPlaylistCommand },
            { "pausemusic", _SpotifyCommandHandler.HandlePauseMusicCommand },
            { "resumemusic", _SpotifyCommandHandler.HandleResumeMusicCommand },
            { "next", _SpotifyCommandHandler.HandleNextSongCommand },
            { "prev", _SpotifyCommandHandler.HandlePrevSongCommand },
            { "queue", _SpotifyCommandHandler.HandleGetQueueCommand },
            { "addsong", () => _SpotifyCommandHandler.HandleAddSongToQueueCommand(commandArgs) },
            { "playsong", () => _SpotifyCommandHandler.HandlePlaySpecificSongCommand(commandArgs, username) },
            { "createredemption", () => _TwitchCommandHandler.HandleCreateRedemptionCommand(commandArgs) },

            { "setscene", () => _ObsCommandHandler.HandleSetSceneCommand(commandArgs) },
            { "mutemic", () => _ObsCommandHandler.HandleSetInputMute("microphone") },
            { "mutemusic", () => _ObsCommandHandler.HandleSetInputMute("spotify") },
            { "standardvolumes", _ObsCommandHandler.HandleSetStandardVolumes },
            { "volumezero", _ObsCommandHandler.HandleVolumeZero },
            
            { "togglewarstats", _ClashCommandHandler.HandleToggleWarStatsCommand },
            { "playertag", () => _ClashCommandHandler.HandleAddPlayerTagCommand(commandArgs) }
        };

        if (_CommandHandlers.TryGetValue(commandText, out var handler))
            handler.Invoke();
        else
            HandleUnknownCommand();
    }

    private void HandleCommandsCommand()
    {
        var commands = "";
        foreach (string command in _CommandHandlers.Keys)
            if (command != _CommandHandlers.Keys.Last())
                commands += $"{command}, ";
            else
                commands += $"{command}";
        _IrcClient.SendPublicChatMessage($"The following commands are available on this channel: {commands}");
    }

    private void HandleUnknownCommand()
    {
        _IrcClient.SendPublicChatMessage("Unknown command");
    }

    private void HandleExitBotCommand(string username)
    {
        if (!username.Equals(TwitchConstants.ChannelName)) return;
        _IrcClient.SendPublicChatMessage(TwitchConstants.BotExitMessage);
        Environment.Exit(0);
    }

    private void HandleAfgeleidCommand()
    {
        var afgeleidtext = _GeneralFileReader.ReadAfgeleidCounter();
        var afgeleid = Convert.ToInt32(afgeleidtext);
        afgeleid++;
        _IrcClient.SendPublicChatMessage($"Spekkie is {afgeleid}x afgeleid geweest");
        _GeneralFileWriter.WriteAfgeleidCounter(afgeleid.ToString());
    }
}