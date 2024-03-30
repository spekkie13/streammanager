using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;

namespace SpekkieTwitchBot.Twitch.Client;

public class CustomClient : IClient
{
    public void Close(bool callDisconnect = true)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public bool Open()
    {
        throw new NotImplementedException();
    }

    public bool Send(string message)
    {
        throw new NotImplementedException();
    }

    public bool SendWhisper(string message)
    {
        throw new NotImplementedException();
    }

    public void Reconnect()
    {
        throw new NotImplementedException();
    }

    public void MessageThrottled(OnMessageThrottledEventArgs eventArgs)
    {
        throw new NotImplementedException();
    }

    public void SendFailed(OnSendFailedEventArgs eventArgs)
    {
        throw new NotImplementedException();
    }

    public void Error(OnErrorEventArgs eventArgs)
    {
        throw new NotImplementedException();
    }

    public void WhisperThrottled(OnWhisperThrottledEventArgs eventArgs)
    {
        throw new NotImplementedException();
    }

    public TimeSpan DefaultKeepAliveInterval { get; set; }
    public int SendQueueLength { get; }
    public int WhisperQueueLength { get; }
    public bool IsConnected { get; }
    public IClientOptions Options { get; }
    public event EventHandler<OnConnectedEventArgs>? OnConnected;
    public event EventHandler<OnDataEventArgs>? OnData;
    public event EventHandler<OnDisconnectedEventArgs>? OnDisconnected;
    public event EventHandler<OnErrorEventArgs>? OnError;
    public event EventHandler<OnFatalErrorEventArgs>? OnFatality;
    public event EventHandler<OnMessageEventArgs>? OnMessage;
    public event EventHandler<OnMessageThrottledEventArgs>? OnMessageThrottled;
    public event EventHandler<OnWhisperThrottledEventArgs>? OnWhisperThrottled;
    public event EventHandler<OnSendFailedEventArgs>? OnSendFailed;
    public event EventHandler<OnStateChangedEventArgs>? OnStateChanged;
    public event EventHandler<OnReconnectedEventArgs>? OnReconnected;
}