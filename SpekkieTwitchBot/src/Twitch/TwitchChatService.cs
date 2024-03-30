using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Twitch.Commands;

namespace SpekkieTwitchBot.Twitch;

public class TwitchChatService : BackgroundService
{
    private readonly IrcClient _IrcClient;
    private readonly SpotifyCommandHandler _SpotifyCommandHandler;
    private readonly TextCommandHandler _TextCommandHandler;
    private readonly TimerCommandHandler _TimerCommandHandler;
    private static Dictionary<string, Action> _CommandHandlers = new ();
    private const string BroadcasterName = "spekkie1313";

    public TwitchChatService(
        IrcClient ircClient, 
        SpotifyCommandHandler spotifyCommandHandler, 
        TextCommandHandler textCommandHandler, 
        TimerCommandHandler timerCommandHandler)
    {
        _IrcClient = ircClient;
        _SpotifyCommandHandler = spotifyCommandHandler;
        _TextCommandHandler = textCommandHandler;
        _TimerCommandHandler = timerCommandHandler;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // while (!stoppingToken.IsCancellationRequested)
        // {
        //     string twitchMessage = _IrcClient.ReadMessage();
        //     Console.WriteLine(twitchMessage);
        //
        //     if (twitchMessage.Contains('!'))
        //         HandleCommand(twitchMessage);
        //
        //     await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        // }
    }
    
    private void HandleCommand(string message)
    {
        int indexParseSign = message.IndexOf("!", StringComparison.Ordinal);
        string username = message[1..indexParseSign];

        indexParseSign = message.IndexOf(" :", StringComparison.Ordinal);
        message = message[(indexParseSign + 2)..];
        string command = message.Split(" ").First();

        if (!command.StartsWith("!")) return;

        _CommandHandlers = new Dictionary<string, Action>
        {
            { "!commands", HandleCommandsCommand },
            { "!exitbot", () => HandleExitBotCommand(username) },
            { "!afgeleid", HandleAfgeleidCommand},
            { "!hello", _TextCommandHandler.HandleHelloCommand },
            { "!twitter", _TextCommandHandler.HandleGetTwitterCommand },
            { "!youtube", _TextCommandHandler.HandleGetYouTubeCommand },
            { "!discord", _TextCommandHandler.HandleGetDiscordCommand },
            { "!lurk", () => _TextCommandHandler.HandleLurkCommand(username) },
            { "!tag", _TextCommandHandler.HandleGetCocTagCommand },

            { "!pausetimer", _TimerCommandHandler.HandlePauseTimerCommand },
            { "!starttimer", _TimerCommandHandler.HandleStartTimerCommand },
            { "!addtime", () => _TimerCommandHandler.HandleAddTimeToTimerCommand(message.Split(' ')[1]) },
            { "!settime", () => _TimerCommandHandler.HandleSetTimeOnTimerCommand(message.Split(' ')[1]) },

            { "!song", _SpotifyCommandHandler.HandleGetCurrentSongCommand },
            { "!playlist", _SpotifyCommandHandler.HandleGetCurrentPlaylistCommand },
            { "!pausemusic", _SpotifyCommandHandler.HandlePauseMusicCommand },
            { "!resumemusic", _SpotifyCommandHandler.HandleResumeMusicCommand },
            { "!next", _SpotifyCommandHandler.HandleNextSongCommand },
            { "!prev", _SpotifyCommandHandler.HandlePrevSongCommand },
            { "!queue", _SpotifyCommandHandler.HandleGetQueueCommand },
            { "!addsong", () => _SpotifyCommandHandler.HandleAddSongToQueueCommand(message.Split(' ')[1]) },
            { "!playsong", () => _SpotifyCommandHandler.HandlePlaySpecificSongCommand(message.Split(' ')[1], username) },
            { "!playsound", () => _SpotifyCommandHandler.PlaySound() },
        };

        if (_CommandHandlers.TryGetValue(command, out Action? handler))
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