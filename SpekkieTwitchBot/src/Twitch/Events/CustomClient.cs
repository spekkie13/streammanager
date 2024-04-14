#nullable disable
using System.Net.Security;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Services;

namespace SpekkieTwitchBot.Twitch.Events;

public class CustomClient : IClient
{
    private int _notConnectedCounter;
    private const string Server = "irc.chat.twitch.tv";
    private StreamReader _reader;
    private StreamWriter _writer;
    private readonly Throttlers _throttlers;
    private CancellationTokenSource _tokenSource = new();
    private bool _stopServices;
    private bool _networkServicesRunning;
    private Task[] _networkTasks;
    private Task _monitorTask;

    public TimeSpan DefaultKeepAliveInterval { get; set; }

    public int SendQueueLength => _throttlers.SendQueue.Count;

    public int WhisperQueueLength => _throttlers.WhisperQueue.Count;

    public bool IsConnected
    {
        get
        {
            System.Net.Sockets.TcpClient client = Client;
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

    private int Port
    {
        get
        {
            if (Options == null)
                return 0;
            return !Options.UseSsl ? 80 : 443;
        }
    }

    private System.Net.Sockets.TcpClient Client { get; set; }

    public CustomClient(IClientOptions options = null)
    {
        Options = options ?? new ClientOptions();
        _throttlers = new Throttlers(this, Options.ThrottlingPeriod, Options.WhisperThrottlingPeriod)
        {
            TokenSource = _tokenSource
        };
        InitializeClient();
    }

    private void InitializeClient()
    {
        Client = new System.Net.Sockets.TcpClient();
        if (_monitorTask == null)
        {
            _monitorTask = StartMonitorTask();
        }
        else
        {
            if (!_monitorTask.IsCompleted)
                return;
            _monitorTask = StartMonitorTask();
        }
    }

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
                    _reader = new StreamReader(sslStream);
                    _writer = new StreamWriter(sslStream);
                }
                else
                {
                    _reader = new StreamReader(Client.GetStream());
                    _writer = new StreamWriter(Client.GetStream());
                }
            })).Wait(10000);
            if (!IsConnected)
                return Open();
            StartNetworkServices();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            InitializeClient();
            return false;
        }
    }

    public void Close(bool callDisconnect = true)
    {
        _reader?.Dispose();
        _writer?.Dispose();
        Client?.Close();
        _stopServices = callDisconnect;
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
            _throttlers.SendQueue.Add(new Tuple<DateTime, string>(DateTime.UtcNow, message));
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
            _throttlers.WhisperQueue.Add(new Tuple<DateTime, string>(DateTime.UtcNow, message));
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

    private void StartNetworkServices()
    {
        _networkServicesRunning = true;
        _networkTasks = new[]
        {
            StartListenerTask(),
            _throttlers.StartSenderTask(),
            _throttlers.StartWhisperSenderTask()
        }.ToArray();
        if (!_networkTasks.Any((Func<Task, bool>)(c => c.IsFaulted)))
            return;
        _networkServicesRunning = false;
        CleanupServices();
    }

    public Task SendAsync(string message)
    {
        return Task.Run((Func<Task>)(async () =>
        {
            await _writer.WriteLineAsync(message);
            await _writer.FlushAsync();
        }));
    }

    private Task StartListenerTask()
    {
        return Task.Run((Func<Task>)(async () =>
        {
            CustomClient sender = this;
            while (sender.IsConnected)
            {
                if (!sender._networkServicesRunning)
                    break;
                try
                {
                    string str = await sender._reader.ReadLineAsync();
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
                while (!_tokenSource.IsCancellationRequested)
                {
                    if (isConnected == IsConnected)
                    {
                        Thread.Sleep(200);
                        if (!IsConnected)
                            ++_notConnectedCounter;
                        else
                            ++num;
                        if (num >= 300)
                        {
                            Send("PING");
                            num = 0;
                        }

                        switch (_notConnectedCounter)
                        {
                            case 25:
                            case 75:
                            case 150:
                            case 300:
                            case 600:
                                Reconnect();
                                break;
                            default:
                                if (_notConnectedCounter >= 1200 && _notConnectedCounter % 600 == 0)
                                {
                                    Reconnect();
                                }

                                break;
                        }

                        if (_notConnectedCounter != 0 && IsConnected)
                            _notConnectedCounter = 0;
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

                        if (!IsConnected && !_stopServices)
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

            if (!flag || _stopServices)
                return;
            Reconnect();
        }), _tokenSource.Token);
    }

    private void CleanupServices()
    {
        _tokenSource.Cancel();
        _tokenSource = new CancellationTokenSource();
        _throttlers.TokenSource = _tokenSource;
        if (!_stopServices)
            return;
        Task[] networkTasks = _networkTasks;
        if ((networkTasks != null ? (networkTasks.Length != 0 ? 1 : 0) : 0) == 0 ||
            Task.WaitAll(_networkTasks, 15000))
            return;
        EventHandler<OnFatalErrorEventArgs> onFatality = OnFatality;
        if (onFatality != null)
            onFatality(this, new OnFatalErrorEventArgs
            {
                Reason = "Fatal network error. Network services fail to shut down."
            });
        _stopServices = false;
        _throttlers.Reconnecting = false;
        _networkServicesRunning = false;
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
        _throttlers.ShouldDispose = true;
        _tokenSource.Cancel();
        Thread.Sleep(500);
        _tokenSource.Dispose();
        Client?.Dispose();
        GC.Collect();
    }
}