using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Twitch;
using SpekkieTwitchBot.Twitch;
using SpekkieTwitchBot.Twitch.Client;
using SpekkieTwitchBot.Twitch.Commands;
using SpekkieTwitchBot.Twitch.Pubsub;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models.Responses.Messages.Redemption;
using AuthorizationCredentials = SpekkieTwitchBot.Models.Twitch.AuthorizationCredentials;
using OnChannelPointsRewardRedeemedArgs = SpekkieTwitchBot.Models.Twitch.Pubsub.Args.OnChannelPointsRewardRedeemedArgs;

namespace SpekkieTwitchBot.Web;

public class TwitchWebsocketService : IHostedService
{
    private readonly IConfiguration _Configuration;
    private readonly ILogger<TwitchWebsocketService> _Logger;
    private readonly CustomTwitchClient _TwitchClient;
    private readonly CustomPubsub _TwitchPubSub;
    private TwitchAuth _TwitchAuth;
    private IrcClient _IrcClient;
    private SpotifyCommandHandler _SpotifyCommandHandler;
    private TextCommandHandler _TextCommandHandler;
    private TimerCommandHandler _TimerCommandHandler;
    private const string BroadcasterName = "spekkie1313";
    private static Dictionary<string, Action> _CommandHandlers = new ();

    public TwitchWebsocketService(
        IConfiguration configuration, 
        ILogger<TwitchWebsocketService> logger,
        CustomTwitchClient twitchClient, 
        CustomPubsub twitchPubSub,
        IrcClient ircClient, 
        SpotifyCommandHandler spotifyCommandHandler, 
        TextCommandHandler textCommandHandler, 
        TimerCommandHandler timerCommandHandler)
    {
        _Configuration = configuration;
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _IrcClient = ircClient;
        _SpotifyCommandHandler = spotifyCommandHandler;
        _TextCommandHandler = textCommandHandler;
        _TimerCommandHandler = timerCommandHandler;

        _TwitchClient = twitchClient ?? throw new ArgumentNullException(nameof(twitchClient));
        _TwitchPubSub = twitchPubSub ?? throw new ArgumentNullException(nameof(twitchPubSub));

        SetupAuth();
        SetupTwitchClient();
        SetupPubSub();
    }

    private void SetupAuth()
    {
        _TwitchAuth = AuthUtils.GetTwitchAuth();
        AuthorizationCredentials authCred = GetAuthorizationCredentials().Result ?? new AuthorizationCredentials();
        ClientCredentials clientCred = GetClientCredentials().Result ?? new ClientCredentials();
        _TwitchAuth.AppToken = authCred.access_token;
        _TwitchAuth.UserToken = clientCred.access_token;
    }

    private void SetupTwitchClient()
    {
        ConnectionCredentials cred = new ConnectionCredentials("spekkie1313", _TwitchAuth.Implicit_OAuth);
        _TwitchClient.Initialize(cred, _TwitchAuth.BroadcasterName);
        _TwitchClient.OnCommunitySubscription += OnCommunitySubscription;
        _TwitchClient.OnNewSubscriber += OnNewSubscriber;
        _TwitchClient.OnGiftedSubscription += OnGiftedSubscription;
        _TwitchClient.OnReSubscriber += OnReSubscriber;
        _TwitchClient.OnContinuedGiftedSubscription += OnContinuedGiftSubscription;
        _TwitchClient.OnPrimePaidSubscriber += OnPrimePaidSubscriber;
        _TwitchClient.OnChatCommandReceived += OnChatCommandReceived;
    }
    
    private void SetupPubSub()
    {
        _TwitchPubSub.OnPubSubServiceConnected += OnPubSubConnected;
        _TwitchPubSub.OnListenResponse += OnListenResponse;
        _TwitchPubSub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        
        _TwitchPubSub.ListenToVideoPlayback(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToFollows(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToSubscriptions(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToLeaderboards(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToPredictions(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToRaid(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToChannelPoints(_TwitchAuth.ChannelId);
        _TwitchPubSub.ListenToBitsEventsV2(_TwitchAuth.ChannelId);
        
    }

    private void OnPubSubConnected(object? sender, EventArgs e)
    {
        _TwitchPubSub.SendTopics(_TwitchAuth.AppToken);
        _Logger.LogInformation("Connected");
    }

    private void OnListenResponse(object? sender, OnListenResponseArgs e)
    {
        Console.WriteLine(e.Successful
            ? $"Successfully listening to: {e.Topic}"
            : $"Failed to listen! Error: {e.Response.Error}");
    }

    private void OnChannelPointsRewardRedeemed(object? sender, OnChannelPointsRewardRedeemedArgs e)
    {
        Redemption reward = e.RewardRedeemed.Redemption;
        switch (e.RewardRedeemed.Redemption.Reward.Title)
        {
            case "Song Request":
                bool success = HandleSongRequest(reward.UserInput);
                
                break;
            default:
                break;
        }
        Console.WriteLine("Channel Points redeemed");
        Console.WriteLine($"{e.RewardRedeemed.Redemption.Reward.Title}");
    }
    
    private void OnCommunitySubscription(object? sender, OnCommunitySubscriptionArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.GiftedSubscription.DisplayName}");
    }
    
    private void OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.Subscriber}");
    }
    
    private void OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.GiftedSubscription.DisplayName}");
    }
    
    private void OnReSubscriber(object? sender, OnReSubscriberArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.ReSubscriber.DisplayName}");
    }
    
    private void OnContinuedGiftSubscription(object? sender, OnContinuedGiftedSubscriptionArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.ContinuedGiftedSubscription.DisplayName}");
    }
    
    private void OnPrimePaidSubscriber(object? sender, OnPrimePaidSubscriberArgs e)
    {       
        Console.WriteLine("new community sub");
        Console.WriteLine($"{e.PrimePaidSubscriber.DisplayName}");
    }

    private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
    {
        HandleCommand(e.Command);
    }
    
    private void HandleCommand(ChatCommand command)
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
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _TwitchClient.Connect();
        _TwitchPubSub.Connect();
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _TwitchClient.Disconnect();
        _TwitchPubSub.Disconnect();
        return Task.CompletedTask;
    }
    
    private async Task<ClientCredentials?> GetClientCredentials()
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _TwitchAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", _TwitchAuth.ClientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);
            ClientCredentials? cred = JsonConvert.DeserializeObject<ClientCredentials>(responseContent);
            return cred;
        }
        
        Console.WriteLine($"Failed to get access token. Status code: {response.StatusCode}");
        return null;
    }

    private async Task<AuthorizationCredentials?> GetAuthorizationCredentials()
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _TwitchAuth.ClientId),
            new KeyValuePair<string, string>("client_secret", _TwitchAuth.ClientSecret),
            new KeyValuePair<string, string>("code", _TwitchAuth.Code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", "http://localhost:3000/")
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
                AuthorizationCredentials? cred = JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
                UpdateTwitchSettings(cred);
                return cred;
            case HttpStatusCode.BadRequest:
                cred = await RefreshTokenAsync(_TwitchAuth.ClientId, _TwitchAuth.ClientSecret, _TwitchAuth.RefreshToken);
                return cred;
            default:
                Console.WriteLine($"Failed to get tokens. Status code: {response.StatusCode}");
                return null;
        }
    }
    
    async Task<AuthorizationCredentials?> RefreshTokenAsync(string clientId, string clientSecret, string refreshToken)
    {
        using HttpClient client = new HttpClient();
        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", parameters);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<AuthorizationCredentials>(responseContent);
        }

        Console.WriteLine($"Error refreshing token: {response.StatusCode}");
        return null;
    }

    private void UpdateTwitchSettings(AuthorizationCredentials? cred)
    {
        _TwitchAuth.RefreshToken = cred?.refresh_token ?? "";
        string json = JsonConvert.SerializeObject(_TwitchAuth);
        FileHandler.WriteTwitchAuthFile(json);
    }

    private bool HandleSongRequest(string songUrl)
    {
        if (songUrl.Contains("open.spotify.com"))
        {
            _SpotifyCommandHandler.HandleAddSongToQueueCommand(songUrl);
            return true;
        }

        _IrcClient.SendPublicChatMessage("Please provide a valid Spotify link");
        return false;
    }
}