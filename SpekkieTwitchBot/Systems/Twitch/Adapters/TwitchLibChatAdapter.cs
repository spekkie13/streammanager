using Newtonsoft.Json;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using TwitchAuthService.Events;
using TwitchLib.Client.Models;

namespace SpekkieTwitchBot.Systems.Twitch.Adapters;

public class TwitchLibChatAdapter : ITwitchChat
{
    private readonly CustomTwitchClient _Client;
    private readonly TwitchGeneralFile? _Identity;
    
    public event Func<ChatCommandReceived, Task>? OnChatCommandReceived;

    public TwitchLibChatAdapter(CustomTwitchClient client, TwitchFileReader reader)
    {
        _Client = client;
        string json = reader.ReadTwitchGeneralAuthFile();
        _Identity = JsonConvert.DeserializeObject<TwitchGeneralFile>(json);
        
        _Client.OnChatCommandReceived += async (_, e) =>
        {
            var cmd = e.Command;
            var msg = cmd.ChatMessage;

            var ev = new ChatCommandReceived(
                MessageId: msg.Id,
                UserId: msg.UserId,
                Username: msg.Username,
                RawMessage: msg.Message,
                CommandText: cmd.CommandText,             // zonder "!"
                ArgumentsAsString: cmd.ArgumentsAsString  // "a b c"
            );

            if (OnChatCommandReceived is not null)
                await OnChatCommandReceived.Invoke(ev);
        };
    }
    
    public Task ConnectAsync(CancellationToken ct)
    {
        if (!_Client.IsInitialized)
        {
            var channel = (_Identity?.BroadcasterName ?? _Identity?.BotName ?? "").Trim();

            if (string.IsNullOrWhiteSpace(channel))
                throw new InvalidOperationException("No Channel Name found. Expected BroadcasterName or BotName");

            if (channel.StartsWith("#"))
                channel = channel[1..];
            
            var token = (_Identity.ImplicitOAuth ?? "").Trim();
            
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("No OAuth Token found. Expected ImplicitOAuth Token");
            
            if (!token.StartsWith("oauth:", StringComparison.OrdinalIgnoreCase))
                token = "oauth:" + token;
            
            var creds = new ConnectionCredentials(_Identity.BroadcasterName, token);
            
            _Client.Initialize(creds, channel);
        }
        
        _Client.Connect();
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct)
    {
        _Client.Disconnect();
        return Task.CompletedTask;
    }

    public Task ReplyAsync(string replyToMessageId, string message, CancellationToken ct)
    {
        _Client.SendReply(_Identity.ChannelId, replyToMessageId, message);
        return Task.CompletedTask;
    }

    public Task SendAsync(string message, CancellationToken ct)
    {
        _Client.SendMessage(_Identity.ChannelId, message);
        return Task.CompletedTask;
    }
}