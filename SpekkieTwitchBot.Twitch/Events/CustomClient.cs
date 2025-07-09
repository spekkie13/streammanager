#nullable disable
using System.Net.Security;
using System.Net.Sockets;
using SpekkieTwitchBot.General.FileHandling;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Services;

namespace TwitchAuthService.Events;

public class CustomClient : IClient
{
    private const string Server = "irc.chat.twitch.tv";
    private readonly Logger _Logger;
    private readonly Throttlers _Throttlers;
    private Task _MonitorTask;
    private bool _NetworkServicesRunning;
    private Task[] _NetworkTasks;
    private int _NotConnectedCounter;
    private StreamReader _Reader;
    private bool _StopServices;
    private CancellationTokenSource _TokenSource = new();
    private StreamWriter _Writer;

    public CustomClient(Logger logger, IClientOptions options = null)
    {
        _Logger = logger;
        Options = options ?? new ClientOptions();
        _Throttlers = new Throttlers(this, Options.ThrottlingPeriod, Options.WhisperThrottlingPeriod)
        {
            TokenSource = _TokenSource
        };
        InitializeClient();
    }

    private int Port
    {
        get
        {
            if (Options == null)
                return 0;
            return !Options.UseSsl ? 80 : 443;
        }
    }

    private TcpClient Client { get; set; }

    public TimeSpan DefaultKeepAliveInterval { get; set; }

    public int SendQueueLength => _Throttlers.SendQueue.Count;

    public int WhisperQueueLength => _Throttlers.WhisperQueue.Count;

    public bool IsConnected
    {
        get
        {
            TcpClient client = Client;
            return client is { Connected: true };
        }
    }

    public IClientOptions Options { get; }

    public event EventHandler<OnConnectedEventArgs> OnConnected;

    public event EventHandler<OnDataEventArgs> OnData;

    public event EventHandler<OnDisconnectedEventArgs> OnDisconnected;

    public event EventHandler<OnErrorEventArgs> OnError;

    public event EventHandler<OnFatalErrorEventArgs> OnFatality;

    public event EventHandler<OnMessageEventArgs> OnMessage;

    public event EventHandler<OnMessageThrottledEventArgs> OnMessageThrottled;

    public event EventHandler<OnWhisperThrottledEventArgs> OnWhisperThrottled;

    public event EventHandler<OnSendFailedEventArgs> OnSendFailed;

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;

    public event EventHandler<OnReconnectedEventArgs> OnReconnected;

    public bool Open()
    {
        try
        {
            if (IsConnected)
                return true;
            Task.Run((Action)(() =>
            {
                InitializeClient();
                Client.Connect(Server, Port);
                if (Options.UseSsl)
                {
                    SslStream sslStream = new SslStream(Client.GetStream(), false);
                    sslStream.AuthenticateAsClient(Server);
                    _Reader = new StreamReader(sslStream);
                    _Writer = new StreamWriter(sslStream);
                }
                else
                {
                    _Reader = new StreamReader(Client.GetStream());
                    _Writer = new StreamWriter(Client.GetStream());
                }
            })).Wait(10000);
            if (!IsConnected)
                return Open();
            StartNetworkServices();
            return true;
        }
        catch (Exception ex)
        {
            _Logger.LogInfo(ex.Message);
            InitializeClient();
            return false;
        }
    }

    public void Close(bool callDisconnect = true)
    {
        _Reader?.Dispose();
        _Writer?.Dispose();
        Client?.Close();
        _StopServices = callDisconnect;
        CleanupServices();
        InitializeClient();
        EventHandler<OnDisconnectedEventArgs> onDisconnected = OnDisconnected;
        onDisconnected?.Invoke(this, new OnDisconnectedEventArgs());
    }

    public void Reconnect()
    {
        Task.Run((Action)(() =>
        {
            Task.Delay(20).Wait();
            Close();
            if (!Open())
                return;
            EventHandler<OnReconnectedEventArgs> onReconnected = OnReconnected;
            if (onReconnected == null)
                return;
            onReconnected(this, new OnReconnectedEventArgs());
        }));
    }

    public bool Send(string message)
    {
        try
        {
            if (!IsConnected || SendQueueLength >= Options.SendQueueCapacity)
                return false;
            _Throttlers.SendQueue.Add(new Tuple<DateTime, string>(DateTime.UtcNow, message));
            return true;
        }
        catch (Exception ex)
        {
            EventHandler<OnErrorEventArgs> onError = OnError;
            onError?.Invoke(this, new OnErrorEventArgs
            {
                Exception = ex
            });
            throw;
        }
    }

    public bool SendWhisper(string message)
    {
        try
        {
            if (!IsConnected || WhisperQueueLength >= Options.WhisperQueueCapacity)
                return false;
            _Throttlers.WhisperQueue.Add(new Tuple<DateTime, string>(DateTime.UtcNow, message));
            return true;
        }
        catch (Exception ex)
        {
            EventHandler<OnErrorEventArgs> onError = OnError;
            onError?.Invoke(this, new OnErrorEventArgs
            {
                Exception = ex
            });
            throw;
        }
    }

    public void WhisperThrottled(OnWhisperThrottledEventArgs eventArgs)
    {
        EventHandler<OnWhisperThrottledEventArgs> whisperThrottled = OnWhisperThrottled;
        if (whisperThrottled == null)
            return;
        whisperThrottled(this, eventArgs);
    }

    public void MessageThrottled(OnMessageThrottledEventArgs eventArgs)
    {
        EventHandler<OnMessageThrottledEventArgs> messageThrottled = OnMessageThrottled;
        if (messageThrottled == null)
            return;
        messageThrottled(this, eventArgs);
    }

    public void SendFailed(OnSendFailedEventArgs eventArgs)
    {
        EventHandler<OnSendFailedEventArgs> onSendFailed = OnSendFailed;
        if (onSendFailed == null)
            return;
        onSendFailed(this, eventArgs);
    }

    public void Error(OnErrorEventArgs eventArgs)
    {
        EventHandler<OnErrorEventArgs> onError = OnError;
        if (onError == null)
            return;
        onError(this, eventArgs);
    }

    public void Dispose()
    {
        Close();
        _Throttlers.ShouldDispose = true;
        _TokenSource.Cancel();
        Thread.Sleep(500);
        _TokenSource.Dispose();
        Client?.Dispose();
        GC.Collect();
    }

    private void InitializeClient()
    {
        Client = new TcpClient();
        if (_MonitorTask == null)
        {
            _MonitorTask = StartMonitorTask();
        }
        else
        {
            if (!_MonitorTask.IsCompleted)
                return;
            _MonitorTask = StartMonitorTask();
        }
    }

    private void StartNetworkServices()
    {
        _NetworkServicesRunning = true;
        _NetworkTasks =
        [
            StartListenerTask(),
            _Throttlers.StartSenderTask(),
            _Throttlers.StartWhisperSenderTask()
        ];
        if (!_NetworkTasks.Any((Func<Task, bool>)(c => c.IsFaulted)))
            return;
        _NetworkServicesRunning = false;
        CleanupServices();
    }

    public Task SendAsync(string message)
    {
        return Task.Run((Func<Task>)(async () =>
        {
            await _Writer.WriteLineAsync(message);
            await _Writer.FlushAsync();
        }));
    }

    private Task StartListenerTask()
    {
        return Task.Run((Func<Task>)(async () =>
        {
            CustomClient sender = this;
            while (sender.IsConnected)
            {
                if (!sender._NetworkServicesRunning)
                    break;
                try
                {
                    string str = await sender._Reader.ReadLineAsync();
                    if (str == null && sender.IsConnected)
                    {
                        sender.Send("PING");
                        Task.Delay(500).Wait();
                    }

                    EventHandler<OnMessageEventArgs> onMessage = sender.OnMessage;
                    if (onMessage != null)
                        onMessage(sender, new OnMessageEventArgs
                        {
                            Message = str
                        });
                }
                catch (Exception ex)
                {
                    EventHandler<OnErrorEventArgs> onError = sender.OnError;
                    if (onError != null)
                        onError(sender, new OnErrorEventArgs
                        {
                            Exception = ex
                        });
                }
            }
        }));
    }

    private Task StartMonitorTask()
    {
        return Task.Run((Action)(() =>
        {
            bool flag = false;
            int num = 0;
            try
            {
                bool isConnected = IsConnected;
                while (!_TokenSource.IsCancellationRequested)
                    if (isConnected == IsConnected)
                    {
                        Thread.Sleep(200);
                        if (!IsConnected)
                            ++_NotConnectedCounter;
                        else
                            ++num;
                        if (num >= 300)
                        {
                            Send("PING");
                            num = 0;
                        }

                        switch (_NotConnectedCounter)
                        {
                            case 25:
                            case 75:
                            case 150:
                            case 300:
                            case 600:
                                Reconnect();
                                break;
                            default:
                                if (_NotConnectedCounter >= 1200 && _NotConnectedCounter % 600 == 0) Reconnect();

                                break;
                        }

                        if (_NotConnectedCounter != 0 && IsConnected)
                            _NotConnectedCounter = 0;
                    }
                    else
                    {
                        EventHandler<OnStateChangedEventArgs> onStateChanged = OnStateChanged;
                        if (onStateChanged != null)
                            onStateChanged(this, new OnStateChangedEventArgs
                            {
                                IsConnected = IsConnected,
                                WasConnected = isConnected
                            });
                        if (IsConnected)
                        {
                            EventHandler<OnConnectedEventArgs> onConnected = OnConnected;
                            if (onConnected != null)
                                onConnected(this, new OnConnectedEventArgs());
                        }

                        if (!IsConnected && !_StopServices)
                        {
                            if (isConnected && Options.ReconnectionPolicy != null &&
                                !Options.ReconnectionPolicy.AreAttemptsComplete())
                            {
                                flag = true;
                                break;
                            }

                            EventHandler<OnDisconnectedEventArgs> onDisconnected = OnDisconnected;
                            if (onDisconnected != null)
                                onDisconnected(this, new OnDisconnectedEventArgs());
                        }

                        isConnected = IsConnected;
                    }
            }
            catch (Exception ex)
            {
                EventHandler<OnErrorEventArgs> onError = OnError;
                if (onError != null)
                    onError(this, new OnErrorEventArgs
                    {
                        Exception = ex
                    });
            }

            if (!flag || _StopServices)
                return;
            Reconnect();
        }), _TokenSource.Token);
    }

    private void CleanupServices()
    {
        _TokenSource.Cancel();
        _TokenSource = new CancellationTokenSource();
        _Throttlers.TokenSource = _TokenSource;
        if (!_StopServices)
            return;
        Task[] networkTasks = _NetworkTasks;
        if ((networkTasks != null ? networkTasks.Length != 0 ? 1 : 0 : 0) == 0 ||
            Task.WaitAll(_NetworkTasks, 15000))
            return;
        EventHandler<OnFatalErrorEventArgs> onFatality = OnFatality;
        if (onFatality != null)
            onFatality(this, new OnFatalErrorEventArgs
            {
                Reason = "Fatal network error. Network services fail to shut down."
            });
        _StopServices = false;
        _Throttlers.Reconnecting = false;
        _NetworkServicesRunning = false;
    }
}