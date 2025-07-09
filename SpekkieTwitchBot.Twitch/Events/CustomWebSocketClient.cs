#nullable disable
using System.Net.WebSockets;
using System.Text;
using SpekkieTwitchBot.General.FileHandling;
using TwitchAuthService.Events.Pubsub;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;

namespace TwitchAuthService.Events
{
    public class CustomWebSocketClient : IClient
    {
        private int _NotConnectedCounter;
        private readonly CustomThrottlers _Throttlers;
        private CancellationTokenSource _TokenSource = new();
        private bool _NetworkServicesRunning;
        private Task[] _NetworkTasks;
        private Task _MonitorTask;
        private readonly Logger _Logger;

        public TimeSpan DefaultKeepAliveInterval { get; set; }

        public int SendQueueLength => _Throttlers.SendQueue.Count;

        public int WhisperQueueLength => _Throttlers.WhisperQueue.Count;

        public bool IsConnected
        {
            get
            {
                ClientWebSocket client = Client;
                return client is { State: WebSocketState.Open };
            }
        }

        public IClientOptions Options { get; }

        private ClientWebSocket Client { get; set; }

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

        private string Url { get; }

        private static readonly int[] SourceArray = [25, 75, 150, 300, 600, 1200];

        public CustomWebSocketClient(Logger logger, IClientOptions options = null)
        {
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Options = options ?? new ClientOptions();
            switch (Options.ClientType)
            {
                case ClientType.Chat:
                    Url = Options.UseSsl ? "wss://irc-ws.chat.twitch.tv:443" : "ws://irc-ws.chat.twitch.tv:80";
                    break;
                case ClientType.PubSub:
                    Url = Options.UseSsl ? "wss://pubsub-edge.twitch.tv:443" : "ws://pubsub-edge.twitch.tv:80";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _Throttlers = new CustomThrottlers(logger, this, Options.ThrottlingPeriod, Options.WhisperThrottlingPeriod)
            {
                TokenSource = _TokenSource
            };
        }

        private void InitializeClient()
        {
            Client?.Abort();
            Client = new ClientWebSocket();
            if (_MonitorTask is { IsCompleted: false }) return;

            _MonitorTask = StartMonitorTask();
        }

        public bool Open()
        {
            try
            {
                if (IsConnected)
                    return true;
                InitializeClient();
                Client.ConnectAsync(new Uri(Url), _TokenSource.Token).Wait(10000);
                if (!IsConnected)
                    return Open();
                StartNetworkServices();
                return true;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
            catch (WebSocketException)
            {
                InitializeClient();
                return false;
            }
        }

        public void Close(bool callDisconnect = true)
        {
            Client?.Abort();
            CleanupServices();
            InitializeClient();
            EventHandler<OnDisconnectedEventArgs> onDisconnected = OnDisconnected;
            if (onDisconnected == null)
                return;
            onDisconnected(this, new OnDisconnectedEventArgs());
        }

        public void Reconnect()
        {
            Task.Run(() =>
            {
                try
                {
                    Task.Delay(50).Wait();
                    Close();
                    if (!Open())
                    {
                        OnError?.Invoke(this, new OnErrorEventArgs { Exception = new Exception("Reconnect failed") });
                    }
                    else
                    {
                        OnReconnected?.Invoke(this, new OnReconnectedEventArgs());
                    }
                }
                catch (Exception ex)
                {
                    Error(new OnErrorEventArgs { Exception = ex });
                }
            });
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
                Error(new OnErrorEventArgs { Exception = ex });
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
                Error(new OnErrorEventArgs { Exception = ex });
                throw;
            }
        }
    
        private void StartNetworkServices()
        {
            _NetworkServicesRunning = true;
            _NetworkTasks = [StartListenerTask(), _Throttlers.StartSenderTask(), _Throttlers.StartWhisperSenderTask()];

            if (_NetworkTasks.Any(task => task.IsFaulted))
            {
                _NetworkServicesRunning = false;
                CleanupServices();
            }
        }

        private Task StartListenerTask()
        {
            return Task.Run((Func<Task>) (async () =>
            {
                CustomWebSocketClient sender = this;
                string message = "";
                while (sender.IsConnected)
                {
                    if (sender._NetworkServicesRunning)
                    {
                        byte[] buffer = new byte[1024];
                        WebSocketReceiveResult async;
                        try
                        {
                            async = await sender.Client.ReceiveAsync(new ArraySegment<byte>(buffer), sender._TokenSource.Token);
                        }
                        catch
                        {
                            sender.InitializeClient();
                            return;
                        }

                        switch (async.MessageType)
                        {
                            case WebSocketMessageType.Text:
                                if (!async.EndOfMessage)
                                {
                                    message += Encoding.UTF8.GetString(buffer).TrimEnd(new char[1]);
                                    continue;
                                }
                                message += Encoding.UTF8.GetString(buffer).TrimEnd(new char[1]);
                                EventHandler<OnMessageEventArgs> onMessage = sender.OnMessage;
                                if (onMessage != null)
                                {
                                    onMessage(sender, new OnMessageEventArgs
                                    {
                                        Message = message
                                    });
                                }

                                goto case WebSocketMessageType.Binary;
                            case WebSocketMessageType.Binary:
                                message = "";
                                continue;
                            case WebSocketMessageType.Close:
                                // ISSUE: explicit non-virtual call
                                sender.Close();
                                goto case WebSocketMessageType.Binary;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    return;
                }
            }));
        }

        private Task StartMonitorTask()
        {
            return Task.Run(() =>
            {
                try
                {
                    while (!_TokenSource.IsCancellationRequested)
                    {
                        if (!IsConnected)
                        {
                            _NotConnectedCounter++;
                            if (SourceArray.Contains(_NotConnectedCounter))
                            {
                                Reconnect();
                            }
                        }

                        Thread.Sleep(200);
                    }
                }
                catch (Exception ex)
                {
                    Error(new OnErrorEventArgs { Exception = ex });
                }
            }, _TokenSource.Token);
        }
    
        private void CleanupServices()
        {
            _TokenSource.Cancel();
            _TokenSource = new CancellationTokenSource();
            _Throttlers.TokenSource = _TokenSource;
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
            _Logger?.LogError($"Error occured in CustomWebClient: {eventArgs.Exception}");
            OnError?.Invoke(this, eventArgs);
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
    }
}