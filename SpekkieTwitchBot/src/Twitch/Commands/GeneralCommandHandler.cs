using SpekkieTwitchBot.General;
using TwitchLib.Client.Models;

namespace SpekkieTwitchBot.Twitch.Commands;

public class GeneralCommandHandler
{
    private readonly IrcClient _IrcClient;
    private const string BroadcasterName = "spekkie1313";
    private Dictionary<string, Action> _CommandHandlers = new ();
    private readonly TextCommandHandler _TextCommandHandler;
    private readonly TimerCommandHandler _TimerCommandHandler;
    private readonly SpotifyCommandHandler _SpotifyCommandHandler;
    
    public GeneralCommandHandler(IrcClient ircClient, TextCommandHandler textCommandHandler, TimerCommandHandler timerCommandHandler, SpotifyCommandHandler spotifyCommandHandler)
    {
        _IrcClient = ircClient;
        _TextCommandHandler = textCommandHandler;
        _TimerCommandHandler = timerCommandHandler;
        _SpotifyCommandHandler = spotifyCommandHandler;
    }
    
    public void HandleCommand(ChatCommand command)
    {
        string username = command.ChatMessage.DisplayName;
        string commandText = command.CommandText;
        string commandArgs = command.ArgumentsAsString;
        
        _CommandHandlers = new Dictionary<string, Action>
        {
            { "commands", HandleCommandsCommand },
            { "exitbot", () => HandleExitBotCommand(username) },
            { "afgeleid", HandleAfgeleidCommand},
            { "hello", _TextCommandHandler.HandleHelloCommand },
            { "twitter", _TextCommandHandler.HandleGetTwitterCommand },
            { "youtube", _TextCommandHandler.HandleGetYouTubeCommand },
            { "discord", _TextCommandHandler.HandleGetDiscordCommand },
            { "lurk", () => _TextCommandHandler.HandleLurkCommand(username) },
            { "tag", _TextCommandHandler.HandleGetCocTagCommand },

            { "pausetimer", _TimerCommandHandler.HandlePauseTimerCommand },
            { "starttimer", _TimerCommandHandler.HandleStartTimerCommand },
            { "addtime", () => _TimerCommandHandler.HandleAddTimeToTimerCommand(commandArgs) },
            { "settime", () => _TimerCommandHandler.HandleSetTimeOnTimerCommand(commandArgs) },

            { "song", _SpotifyCommandHandler.HandleGetCurrentSongCommand },
            { "playlist", _SpotifyCommandHandler.HandleGetCurrentPlaylistCommand },
            { "pausemusic", _SpotifyCommandHandler.HandlePauseMusicCommand },
            { "resumemusic", _SpotifyCommandHandler.HandleResumeMusicCommand },
            { "next", _SpotifyCommandHandler.HandleNextSongCommand },
            { "prev", _SpotifyCommandHandler.HandlePrevSongCommand },
            { "queue", _SpotifyCommandHandler.HandleGetQueueCommand },
            { "addsong", () => _SpotifyCommandHandler.HandleAddSongToQueueCommand(commandArgs) },
            { "playsong", () => _SpotifyCommandHandler.HandlePlaySpecificSongCommand(commandArgs, username) },
            { "playsound", () => _SpotifyCommandHandler.PlaySound() },
        };

        if (_CommandHandlers.TryGetValue(commandText, out Action? handler))
            handler.Invoke();
        else
            HandleUnknownCommand();
    }

    private void HandleCommandsCommand()
    {
        string commands = "";
        foreach (string command in _CommandHandlers.Keys)
        {
            if (command != _CommandHandlers.Keys.Last())
                commands += $"{command}, ";
            else
                commands += $"{command}";
        }
        _IrcClient.SendPublicChatMessage($"The following commands are available on this channel: {commands}");
    }

    private void HandleUnknownCommand()
    {
        _IrcClient.SendPublicChatMessage("Unknown command");
    }

    private void HandleExitBotCommand(string username)
    {
        if (!username.Equals(BroadcasterName)) return;
        _IrcClient.SendPublicChatMessage("Bye! Have a beautiful time!");
        Environment.Exit(0);
    }

    private void HandleAfgeleidCommand()
    {
        string afgeleidtext = FileHandler.ReadAfgeleidCounter();
        int afgeleid = Convert.ToInt32(afgeleidtext);
        afgeleid++;
        _IrcClient.SendPublicChatMessage($"Spekkie is {afgeleid}x afgeleid geweest");
        FileHandler.WriteAfgeleidCounter(afgeleid.ToString());
    }

}