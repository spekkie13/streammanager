using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch.Events.ChannelPoint;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat.Irc;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat;

public sealed class TwitchIrcChatClient : ITwitchChat
{
    private readonly TwitchIrcWebSocketTransport _Transport;
    private readonly ITwitchAuthTokenProvider _Tokens;
    private readonly ChannelPointsFeature _ChannelPoints;
    private readonly Logger _Log;
    
    private TwitchGeneralFile? _Identity;

    private string _Oauth = "";
    private string _Channel = "";
    private string _Bot = "";
    private bool _Configured;

    public event Func<ChatCommandReceived, Task>? OnChatCommandReceived;
    public event Func<ChatMessageReceived, Task>? OnChatMessageReceived;

    public TwitchIrcChatClient(
        TwitchIrcWebSocketTransport transport,
        ChannelPointsFeature channelPoints,
        ITwitchAuthTokenProvider tokens,
        Logger log)
    {
        _Transport = transport;
        _Tokens = tokens;
        _ChannelPoints = channelPoints;
        _Log = log;

        _Transport.OnRawLine += HandleRawLineAsync;
        _Transport.OnError += ex => _Log.LogError($"[IRC] {ex}");
        _Transport.OnDisconnected += reason => _Log.LogWarning($"[IRC] Disconnected: {reason}");
    }


    public async Task ConnectAsync(CancellationToken ct)
    {
        await EnsureConfigured(ct);
        await _Transport.ConnectAsync(ct).ConfigureAwait(false);
        await _Transport.SendRawAsync($"PASS {_Oauth}", ct).ConfigureAwait(false);
        await _Transport.SendRawAsync($"NICK {_Bot}", ct).ConfigureAwait(false);
        await _Transport.SendRawAsync("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership", ct)
            .ConfigureAwait(false);
        await _Transport.SendRawAsync($"JOIN #{_Channel}", ct).ConfigureAwait(false);
    }


    public Task DisconnectAsync(CancellationToken ct) => _Transport.DisconnectAsync(ct);

    public Task SendAsync(string message, CancellationToken ct = default)
        => _Transport.SendRawAsync($"PRIVMSG #{_Identity?.BroadcasterName} :{message}", ct);

    public Task ReplyAsync(string replyToMessageId, string message, CancellationToken ct)
        => _Transport.SendRawAsync($"@reply-parent-msg-id={replyToMessageId} PRIVMSG #{_Identity?.BroadcasterName} :{message}", ct);

    private async Task EnsureConfigured(CancellationToken ct)
    {
        if (_Configured) return;

        _Identity = await _Tokens.ReadIdentityAsync(ct);

        _Channel = (_Identity.BroadcasterName ?? _Identity.BotName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(_Channel))
            throw new InvalidOperationException("No Channel Name found. Expected BroadcasterName or BotName");
        if (_Channel.StartsWith("#")) _Channel = _Channel[1..];

        _Bot = (_Identity.BotName ?? _Identity.BroadcasterName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(_Bot))
            throw new InvalidOperationException("No BotName/BroadcasterName found for IRC NICK");

        _Oauth = (_Identity.ImplicitOAuth ?? "").Trim();
        if (string.IsNullOrWhiteSpace(_Oauth))
            throw new InvalidOperationException("No OAuth Token found. Expected ImplicitOAuth Token");
        if (!_Oauth.StartsWith("oauth:", StringComparison.OrdinalIgnoreCase))
            _Oauth = "oauth:" + _Oauth;

        _Log.LogInfo($"[IRC] Configured bot='{_Bot}', channel='#{_Channel}', oauthPrefixOk={_Oauth.StartsWith("oauth:", StringComparison.OrdinalIgnoreCase)}");

        _Configured = true;
    }

    private async Task HandleRawLineAsync(string line)
    {
        // 1) PING/PONG
        if (line.StartsWith("PING", StringComparison.Ordinal))
        {
            try
            {
                await _Transport.SendRawAsync("PONG :tmi.twitch.tv", CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _Log.LogError($"[IRC] Failed PONG: {ex.Message}");
            }
            return;
        }

        // 2) Alleen PRIVMSG voor MVP
        if (!line.Contains("PRIVMSG", StringComparison.Ordinal))
            return;

        // 3) Parse IRC -> domein event
        IrcMessage msg = IrcMessage.Parse(line);

        ChatMessageReceived chat = new ChatMessageReceived(
            MessageId: msg.Tags.GetValueOrDefault("id", ""),
            UserId: msg.Tags.GetValueOrDefault("user-id", ""),
            Username: msg.Tags.GetValueOrDefault("display-name", msg.Username),
            Text: msg.Message,
            CustomRewardId: msg.Tags.GetValueOrDefault("custom-reward-id", "")
        );
        _Log.LogWarning(JsonConvert.SerializeObject(chat));

        if (!string.IsNullOrWhiteSpace(chat.CustomRewardId))
        {
            List<ChannelPointData> redemptions = await _ChannelPoints.GetCustomRedemptions(CancellationToken.None).ConfigureAwait(false);
            ChannelPointData? redemption = redemptions.FirstOrDefault(r => r.Id == chat.CustomRewardId);
            if (redemption == null) return;
        }
        else
        {
            if (OnChatMessageReceived is not null)
                _ = SafeRaise(() => OnChatMessageReceived(chat));
        }


        // 4) Command detectie
        if (!string.IsNullOrWhiteSpace(chat.Text) && chat.Text.StartsWith('!'))
        {
            string[] parts = chat.Text[1..].Split(' ', 2);
            ChatCommandReceived cmd = new ChatCommandReceived(
                MessageId: chat.MessageId,
                UserId: chat.UserId,
                Username: chat.Username,
                RawMessage: chat.Text,
                CommandText: parts[0],
                ArgumentsAsString: parts.Length > 1 ? parts[1] : ""
            );

            if (OnChatCommandReceived is not null)
                _ = SafeRaise(() => OnChatCommandReceived(cmd));
        }

    }

    private async Task SafeRaise(Func<Task> handler)
    {
        try { await handler().ConfigureAwait(false); }
        catch (Exception ex) { _Log.LogError($"[IRC] Handler error: {ex.Message}"); }
    }
}
