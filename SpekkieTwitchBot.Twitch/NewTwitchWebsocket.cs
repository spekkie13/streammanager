using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpekkieClassLibrary.Twitch.Auth;
using Newtonsoft.Json.Linq;

namespace TwitchAuthService;

public class NewTwitchWebsocketService(ILogger<NewTwitchWebsocketService> logger, TwitchAuthService twitchAuthService)
    : BackgroundService
{
    private static readonly string twitchEventSubUrl = "wss://eventsub.wss.twitch.tv/ws";
    private TwitchUserAuth _twitchUserAuth = twitchAuthService.GetTwitchUserAuth();
    private GeneralTwitchAuth _generalTwitchAuth = twitchAuthService.GetGeneralTwitchAuth();
    
    private ClientWebSocket? _ws;
    private int reconnectAttempts = 0;
    private readonly int maxReconnectDelay = 30000; // 30 seconds max backoff
    private CancellationTokenSource _pingCts;

    private readonly List<string> _topicList = new List<string>(); // Stores topics to listen to
    private readonly Dictionary<string, string> _topicToChannelId = new Dictionary<string, string>(); // Map topic to channel ID

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            SetupTopics();
            await SendTopics(_twitchUserAuth.UserToken);
            await ConnectToWebSocket(stoppingToken);
        }
    }

    private async Task ConnectToWebSocket(CancellationToken stoppingToken)
    {
        if (_ws != null)
            if(_ws.State == WebSocketState.Open || _ws.State == WebSocketState.Connecting)
            {
                logger.LogInformation("WebSocket already connected or in progress. Returning.");
                return;
            }

        _ws = new ClientWebSocket();

        try
        {
            logger.LogInformation("Connecting to Twitch EventSub WebSocket...");
            await _ws.ConnectAsync(new Uri(twitchEventSubUrl), stoppingToken);
            logger.LogInformation("Connected!");
        
            reconnectAttempts = 0; // Reset on successful connection

            _pingCts = new CancellationTokenSource();
            _ = Task.Run(async () => await ReceiveMessages(stoppingToken), stoppingToken);

            // Send periodic pings to keep the connection alive
            //_ = Task.Run(async () => await SendPingPong(stoppingToken), stoppingToken);

            while (_ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken); // Adjust as needed for your use case
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"WebSocket error: {ex.Message}");
            await HandleReconnect(stoppingToken);
        }
    }

    private async Task SendPingPong(CancellationToken stoppingToken)
    {
        try
        {
            while (_ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
            {
                var pingMessage = new
                {
                    type = "ping",
                    id = Guid.NewGuid().ToString()
                };
                await _ws.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(pingMessage)), WebSocketMessageType.Text, true, stoppingToken);
                logger.LogInformation("Sent ping message to WebSocket.");

                await Task.Delay(10000, stoppingToken); // Ping every 10 seconds (adjust as needed)
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error sending ping message: {ex.Message}");
        }
    }

    private async Task SendTopics(string oauth = "", bool unlisten = false)
    {
        string nonce = GenerateNonce();
        JArray content = new JArray();

        try
        {
            foreach (var topic in _topicList)
            {
                content.Add(new JValue(topic));
            }

            JObject jobject = new JObject(
                new JProperty("type", !unlisten ? "LISTEN" : "UNLISTEN"),
                new JProperty("nonce", nonce),
                new JProperty("data", new JObject(new JProperty("topics", content)))
            );

            if (!string.IsNullOrEmpty(oauth))
            {
                JObject data = jobject.SelectToken("data") as JObject ?? new JObject();
                data.Add(new JProperty("auth_token", oauth));
                jobject["data"] = data;
            }

            if (_ws.State == WebSocketState.Open)
            {
                await _ws.SendAsync(Encoding.UTF8.GetBytes(jobject.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                logger.LogInformation($"Sent topics: {string.Join(", ", _topicList)}");
                _topicList.Clear();
            }
            else
            {
                logger.LogWarning("WebSocket is not open, cannot send topics.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error sending topics: {ex.Message}");
        }
    }

    private async Task HandleReconnect(CancellationToken stoppingToken)
    {
        reconnectAttempts++;
        int delay = Math.Min(1000 * (int)Math.Pow(2, reconnectAttempts), maxReconnectDelay);
        logger.LogWarning($"Reconnecting in {delay / 1000} seconds...");
        await Task.Delay(delay, stoppingToken);

        await ConnectToWebSocket(stoppingToken);
    }

    private async Task ReceiveMessages(CancellationToken stoppingToken)
    {
        byte[] buffer = new byte[4096];

        while (_ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogWarning("WebSocket closed. Reason: {0}", result.CloseStatusDescription);
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                logger.LogInformation($"Received: {message}");

                // Handle Pong message (keepalive mechanism)
                if (message.Contains("\"type\":\"pong\""))
                {
                    logger.LogInformation("Received Pong response");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error receiving message: {ex.Message}");
                break;
            }
        }
    }

    private string GenerateNonce()
    {
        return Guid.NewGuid().ToString("N");
    }

    public void ListenToFollows(string channelId)
    {
        string topic = "following." + channelId;
        _topicToChannelId[topic] = channelId;
        _topicList.Add(topic);
        logger.LogInformation($"Listening to follows for channel: {channelId}");
    }

    // public void ListenToChatModeratorActions(string userId, string channelId)
    // {
    //     string topic = "chat_moderator_actions." + userId + "." + channelId;
    //     _topicToChannelId[topic] = channelId;
    //     _topicList.Add(topic);
    //     logger.LogInformation($"Listening to chat moderator actions for user: {userId} in channel: {channelId}");
    // }
    //
    // public void ListenToChannelExtensionBroadcast(string channelId, string extensionId)
    // {
    //     string topic = "channel-ext-v1." + channelId + "-" + extensionId + "-broadcast";
    //     _topicToChannelId[topic] = channelId;
    //     _topicList.Add(topic);
    //     logger.LogInformation($"Listening to extension broadcasts for channel: {channelId}, extension: {extensionId}");
    // }
    //
    // public void ListenToBitsEvents(string channelTwitchId)
    // {
    //     string topic = "channel-bits-events-v1." + channelTwitchId;
    //     _topicToChannelId[topic] = channelTwitchId;
    //     _topicList.Add(topic);
    //     logger.LogInformation($"Listening to bits events for channel: {channelTwitchId}");
    // }
    //
    // public void ListenToWhispers(string channelTwitchId)
    // {
    //     string topic = "whispers." + channelTwitchId;
    //     _topicToChannelId[topic] = channelTwitchId;
    //     _topicList.Add(topic);
    //     logger.LogInformation($"Listening to whispers for channel: {channelTwitchId}");
    // }

    public void ListenToVideoPlayback(string channelTwitchId)
    {
        string topic = "video-playback-by-id." + channelTwitchId;
        _topicToChannelId[topic] = channelTwitchId;
        _topicList.Add(topic);
        logger.LogInformation($"Listening to video playback for channel: {channelTwitchId}");
    }
    
    public void ListenToChannelPoints(string channelTwitchId)
    {
        string topic = "channel-points-channel-v1." + channelTwitchId;
        _topicToChannelId[topic] = channelTwitchId;
        _topicList.Add(topic);
        logger.LogInformation($"Listening to channel points for channel: {channelTwitchId}");
    }

    public void ListenToLeaderboards(string channelTwitchId)
    {
        string topic1 = "leaderboard-events-v1.bits-usage-by-channel-v1-" + channelTwitchId + "-WEEK";
        string topic2 = "leaderboard-events-v1.sub-gift-sent-" + channelTwitchId + "-WEEK";
        _topicToChannelId[topic1] = channelTwitchId;
        _topicToChannelId[topic2] = channelTwitchId;
        _topicList.Add(topic1);
        _topicList.Add(topic2);
        logger.LogInformation($"Listening to leaderboards for channel: {channelTwitchId}");
    }

    public void ListenToRaid(string channelTwitchId)
    {
        string topic = "raid." + channelTwitchId;
        _topicToChannelId[topic] = channelTwitchId;
        _topicList.Add(topic);
        logger.LogInformation($"Listening to raids for channel: {channelTwitchId}");
    }

    public void ListenToSubscriptions(string channelId)
    {
        string topic = "channel-subscribe-events-v1." + channelId;
        _topicToChannelId[topic] = channelId;
        _topicList.Add(topic);
        logger.LogInformation($"Listening to subscriptions for channel: {channelId}");
    }

    public void ListenToPredictions(string channelTwitchId)
    {
        string topic = "predictions-channel-v1." + channelTwitchId;
        _topicToChannelId[topic] = channelTwitchId;
        _topicList.Add(topic);
        logger.LogInformation($"Listening to predictions for channel: {channelTwitchId}");
    }
    
    public void ListenToBitsEventsV2(string channelTwitchId)
    {
        string str = "channel-bits-events-v2." + channelTwitchId;
        _topicToChannelId[str] = channelTwitchId;
        ListenToTopic(str);
    }
    
    private void ListenToTopic(string topic)
    {
        _topicList.Add(topic);
    }

    private void SetupTopics()
    {
        ListenToVideoPlayback(_generalTwitchAuth.ChannelId);
        ListenToFollows(_generalTwitchAuth.ChannelId);
        ListenToSubscriptions(_generalTwitchAuth.ChannelId);
        ListenToLeaderboards(_generalTwitchAuth.ChannelId);
        ListenToPredictions(_generalTwitchAuth.ChannelId);
        ListenToRaid(_generalTwitchAuth.ChannelId);
        ListenToChannelPoints(_generalTwitchAuth.ChannelId);
        ListenToBitsEventsV2(_generalTwitchAuth.ChannelId);
    }
}