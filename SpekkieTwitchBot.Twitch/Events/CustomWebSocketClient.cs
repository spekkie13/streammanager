#nullable disable
using System.Net.WebSockets;
using System.Text;
using TwitchAuthService.Events.Pubsub;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;

namespace TwitchAuthService.Events
{
    public class CustomWebSocketClient : IClient
    {
        private int _notConnectedCounter;
        private readonly CustomThrottlers _throttlers;
        private CancellationTokenSource _tokenSource = new();
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

        public CustomWebSocketClient(IClientOptions options = null)
        {
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
            _throttlers = new CustomThrottlers(this, Options.ThrottlingPeriod, Options.WhisperThrottlingPeriod)
            {
                TokenSource = _tokenSource
            };
        }

        private void InitializeClient()
        {
            Client?.Abort();
            Client = new ClientWebSocket();
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
                InitializeClient();
                Client.ConnectAsync(new Uri(Url), _tokenSource.Token).Wait(10000);
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
            Console.WriteLine("Reconnect() method called...");
            Task.Run(() =>
            {
                try
                {
                    Task.Delay(50).Wait();
                    Close();
                    Console.WriteLine("Connection closed, attempting to open...");
                    if (!Open())
                    {
                        Console.WriteLine("Reconnect failed, triggering OnError...");
                        OnError?.Invoke(this, new OnErrorEventArgs { Exception = new Exception("Reconnect failed") });
                    }
                    else
                    {
                        Console.WriteLine("Reconnect successful!");
                        OnReconnected?.Invoke(this, new OnReconnectedEventArgs());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in Reconnect(): {ex.StackTrace}");
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

                _throttlers.SendQueue.Add(new Tuple<DateTime, string>(DateTime.UtcNow, message));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Error in Send()");
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

                _throttlers.WhisperQueue.Add(new Tuple<DateTime, string>(DateTime.UtcNow, message));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Error in SendWhisper()");
                Error(new OnErrorEventArgs { Exception = ex });
                throw;
            }
        }
    
        private void StartNetworkServices()
        {
            Console.WriteLine("Starting network services...");
            _networkServicesRunning = true;
            _networkTasks = [StartListenerTask(), _throttlers.StartSenderTask(), _throttlers.StartWhisperSenderTask()];

            if (_networkTasks.Any(task => task.IsFaulted))
            {
                Console.WriteLine("Network services encountered an error. Stopping...");
                _networkServicesRunning = false;
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
                    if (sender._networkServicesRunning)
                    {
                        byte[] buffer = new byte[1024];
                        WebSocketReceiveResult async;
                        try
                        {
                            async = await sender.Client.ReceiveAsync(new ArraySegment<byte>(buffer), sender._tokenSource.Token);
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
                    Console.WriteLine("Starting monitor task...");
                    while (!_tokenSource.IsCancellationRequested)
                    {
                        if (!IsConnected)
                        {
                            _notConnectedCounter++;
                            if (SourceArray.Contains(_notConnectedCounter))
                            {
                                Console.WriteLine($"Attempting reconnect, counter: {_notConnectedCounter}");
                                Reconnect();
                            }
                        }

                        Thread.Sleep(200);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("Error in StartMonitorTask()");
                    Error(new OnErrorEventArgs { Exception = ex });
                }
            }, _tokenSource.Token);
        }
    
        private void CleanupServices()
        {
            Console.WriteLine("Cleaning up services...");
            _tokenSource.Cancel();
            _tokenSource = new CancellationTokenSource();
            _throttlers.TokenSource = _tokenSource;
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
            Console.WriteLine("On error in Error()");
            OnError?.Invoke(this, eventArgs);
        }
    
        public void Dispose()
        {
            Console.WriteLine("Disposing WebSocket client...");
            Close();
            _throttlers.ShouldDispose = true;
            _tokenSource.Cancel();
            Thread.Sleep(500);
            _tokenSource.Dispose();
            Client?.Dispose();
            GC.Collect();
        }  
    }
}