using System.Net.WebSockets;
using System.Text;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat.Irc;

public sealed class TwitchIrcWebSocketChat
{
    private readonly ClientWebSocket _Ws = new();
    private readonly Uri _Uri = new("wss://irc-ws.chat.twitch.tv:443");

    private string _botUsername = "";
    private string _oauth = "";
    private string _channel = "";

    public event Func<ChatMessageReceived, Task>? OnChatMessageReceived;
    public event Func<ChatCommandReceived, Task>? OnChatCommandReceived;

    public void Configure(string botUsername, string oauth, string channel)
    {
        _botUsername = botUsername;
        _oauth = oauth.StartsWith("oauth:") ? oauth : $"oauth:{oauth}";
        _channel = channel.StartsWith("#") ? channel[1..] : channel;
    }

    public async Task ConnectAsync(CancellationToken ct)
    {
        await _Ws.ConnectAsync(_Uri, ct);

        await SendRawAsync($"PASS {_oauth}", ct);
        await SendRawAsync($"NICK {_botUsername}", ct);
        await SendRawAsync("CAP REQ :twitch.tv/tags twitch.tv/commands", ct);
        await SendRawAsync($"JOIN #{_channel}", ct);

        _ = Task.Run(() => ReceiveLoop(ct), ct);
    }

    public async Task DisconnectAsync(CancellationToken ct)
    {
        if (_Ws.State == WebSocketState.Open)
            await _Ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct);
    }

    public Task SendAsync(string message, CancellationToken ct)
        => SendRawAsync($"PRIVMSG #{_channel} :{message}", ct);

    public Task ReplyAsync(string replyToId, string message, CancellationToken ct)
        => SendRawAsync($"@reply-parent-msg-id={replyToId} PRIVMSG #{_channel} :{message}", ct);

    private async Task SendRawAsync(string msg, CancellationToken ct)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(msg + "\r\n");
        await _Ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        byte[] buffer = new byte[16 * 1024];
        var sb = new StringBuilder();

        while (!ct.IsCancellationRequested && _Ws.State == WebSocketState.Open)
        {
            var result = await _Ws.ReceiveAsync(buffer, ct);

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (!result.EndOfMessage) continue;

            string raw = sb.ToString();
            sb.Clear();

            foreach (string line in raw.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                await HandleLine(line, ct);
        }
    }

    private async Task HandleLine(string line, CancellationToken ct)
    {
        if (line.StartsWith("PING"))
        {
            await SendRawAsync("PONG :tmi.twitch.tv", ct);
            return;
        }

        if (!line.Contains("PRIVMSG")) return;

        IrcMessage msg = IrcMessage.Parse(line);

        ChatMessageReceived chat = new(
            MessageId: msg.Tags.GetValueOrDefault("id", ""),
            UserId: msg.Tags.GetValueOrDefault("user-id", ""),
            Username: msg.Username,
            Text: msg.Message,
            CustomRewardId: msg.Tags.GetValueOrDefault("custom-reward-id", "")
        );

        if (OnChatMessageReceived is not null)
            await OnChatMessageReceived(chat);

        if (chat.Text.StartsWith("!"))
        {
            string[] parts = chat.Text[1..].Split(' ', 2);
            ChatCommandReceived cmd = new(
                MessageId: chat.MessageId,
                UserId: chat.UserId,
                Username: chat.Username,
                RawMessage: chat.Text,
                CommandText: parts[0],
                ArgumentsAsString: parts.Length > 1 ? parts[1] : ""
            );

            if (OnChatCommandReceived is not null)
                await OnChatCommandReceived(cmd);
        }
    }
}
