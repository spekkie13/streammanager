using System.Net.Http.Headers;
using System.Text;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.Constants;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Models.Twitch.Auth;
using SpekkieTwitchBot.Twitch.General;
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
    private readonly GeneralFileReader _GeneralFileReader;
    private readonly GeneralFileWriter _GeneralFileWriter;
    private readonly TwitchAuthService _TwitchAuthService;
    
    public GeneralCommandHandler(
        IrcClient ircClient, 
        TwitchAuthService twitchAuthService,
        GeneralFileReader generalFileReader,
        GeneralFileWriter generalFileWriter,
        TextCommandHandler textCommandHandler, 
        TimerCommandHandler timerCommandHandler, 
        SpotifyCommandHandler spotifyCommandHandler)
    {
        _IrcClient = ircClient;
        _GeneralFileReader = generalFileReader;
        _GeneralFileWriter = generalFileWriter;
        _TextCommandHandler = textCommandHandler;
        _TimerCommandHandler = timerCommandHandler;
        _SpotifyCommandHandler = spotifyCommandHandler;
        _TwitchAuthService = twitchAuthService;
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
            { "refund", () => HandleRefundCommand(username) },
            
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
            { "createredemption", () => HandleCreateRedemptionCommand(commandArgs) }
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
        _IrcClient.SendPublicChatMessage(TwitchConstants.BotExitMessage);
        Environment.Exit(0);
    }

    private void HandleAfgeleidCommand()
    {
        string afgeleidtext = _GeneralFileReader.ReadAfgeleidCounter();
        int afgeleid = Convert.ToInt32(afgeleidtext);
        afgeleid++;
        _IrcClient.SendPublicChatMessage($"Spekkie is {afgeleid}x afgeleid geweest");
        _GeneralFileWriter.WriteAfgeleidCounter(afgeleid.ToString());
    }

    private void HandleRefundCommand(string username)
    {
        if (string.IsNullOrEmpty(username)) return;
    }

    private void HandleCreateRedemptionCommand(string commandArgs)
    {
        string title = commandArgs.Split(" ")[0];
        int cost = Convert.ToInt32(commandArgs.Split(" ")[1]);
        const string Url = "https://api.twitch.tv/helix/channel_points/custom_rewards?broadcaster_id=30731359";
        using HttpClient client = new HttpClient();
        TwitchUserAuth auth = _TwitchAuthService.GetTwitchUserAuth();
        
        client.DefaultRequestHeaders.Add("client-id", auth.ClientId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.UserToken);
        
        var requestContent = new StringContent($"{{\"title\":\"{title}\"\"cost\":{cost}}}", 
            Encoding.UTF8, 
            "application/json");

        var message = client.PostAsync(Url, requestContent).Result;
        var responseContent = message.Content.ReadAsStreamAsync().Result;
    }
}