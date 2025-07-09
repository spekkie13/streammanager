#nullable disable
using System.Collections.Concurrent;
using System.Text;
using SpekkieTwitchBot.General.FileHandling;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Interfaces;

namespace TwitchAuthService.Events.Pubsub
{
    public class CustomThrottlers(Logger logger, IClient client, TimeSpan throttlingPeriod, TimeSpan whisperThrottlingPeriod)
    {
        public readonly BlockingCollection<Tuple<DateTime, string>> SendQueue = new BlockingCollection<Tuple<DateTime, string>>();
        public readonly BlockingCollection<Tuple<DateTime, string>> WhisperQueue = new BlockingCollection<Tuple<DateTime, string>>();
        public bool ResetThrottlerRunning;
        public bool ResetWhisperThrottlerRunning;
        private int _SentCount;
        private int _WhispersSent;
        public Task ResetThrottler;
        public Task ResetWhisperThrottler;

        public bool Reconnecting { get; set; }

        public bool ShouldDispose { get; set; }

        public CancellationTokenSource TokenSource { get; set; }

        private void StartThrottlingWindowReset()
        {
            ResetThrottler = Task.Run((Func<Task<Task>>) (async () =>
            {
                ResetThrottlerRunning = true;
                while (!ShouldDispose && !Reconnecting)
                {
                    Interlocked.Exchange(ref _SentCount, 0);
                    await Task.Delay(throttlingPeriod, TokenSource.Token);
                }
                ResetThrottlerRunning = false;
                return Task.CompletedTask;
            }));
        }

        private void StartWhisperThrottlingWindowReset()
        {
            ResetWhisperThrottler = Task.Run((Func<Task<Task>>) (async () =>
            {
                ResetWhisperThrottlerRunning = true;
                while (!ShouldDispose && !Reconnecting)
                {
                    Interlocked.Exchange(ref _WhispersSent, 0);
                    await Task.Delay(whisperThrottlingPeriod, TokenSource.Token);
                }
                ResetWhisperThrottlerRunning = false;
                return Task.CompletedTask;
            }));
        }

        private void IncrementSentCount() => Interlocked.Increment(ref _SentCount);

        private void IncrementWhisperCount() => Interlocked.Increment(ref _WhispersSent);

        public Task StartSenderTask()
        {
            StartThrottlingWindowReset();
            return Task.Run(async () =>
            {
                try
                {
                    while (!ShouldDispose)
                    {
                        await Task.Delay(client.Options.SendDelay);
                
                        if (_SentCount >= client.Options.MessagesAllowedInPeriod)
                        {
                            LogThrottle("Message");
                            continue;
                        }

                        if (!client.IsConnected || ShouldDispose)
                            continue;
                        
                        if(!TokenSource.Token.IsCancellationRequested)
                            await ProcessMessage(SendQueue, IncrementSentCount);
                    }
                }
                catch (Exception ex)
                {
                    HandleError(ex, "Sender Task encountered an error.");
                }
            });
        }

        public Task StartWhisperSenderTask()
        {
            StartWhisperThrottlingWindowReset();
            return Task.Run(async () =>
            {
                try
                {
                    while (!ShouldDispose)
                    {
                        await Task.Delay(client.Options.SendDelay);
                
                        if (_WhispersSent >= client.Options.WhispersAllowedInPeriod)
                        {
                            LogThrottle("Whisper");
                            continue;
                        }

                        if (!client.IsConnected || ShouldDispose)
                            continue;

                        if(!TokenSource.Token.IsCancellationRequested)
                            await ProcessMessage(WhisperQueue, IncrementWhisperCount);
                    }
                }
                catch (Exception ex)
                {
                    HandleError(ex, "Whisper Sender Task encountered an error.");
                }
            });
        }

        private void LogThrottle(string messageType)
        {
            client.MessageThrottled(new OnMessageThrottledEventArgs
            {
                Message = $"{messageType} Throttle Occurred. Too many {messageType.ToLower()}s within the period specified.",
                AllowedInPeriod = messageType == "Message" ? client.Options.MessagesAllowedInPeriod : client.Options.WhispersAllowedInPeriod,
                Period = messageType == "Message" ? client.Options.ThrottlingPeriod : client.Options.WhisperThrottlingPeriod,
                SentMessageCount = messageType == "Message" ? Interlocked.CompareExchange(ref _SentCount, 0, 0) : Interlocked.CompareExchange(ref _WhispersSent, 0, 0)
            });
        }

        private async Task ProcessMessage(BlockingCollection<Tuple<DateTime, string>> queue, Action incrementCounter)
        {
            try
            {
                if (!queue.TryTake(out var msg, 100, TokenSource.Token))
                {
                    return; // No message available
                }

                if (msg.Item1.Add(client.Options.SendCacheItemTimeout) >= DateTime.UtcNow)
                {
                    await SendMessage(msg.Item2);
                    incrementCounter();
                }
            }
            catch (OperationCanceledException)
            {
                // Gracefully handle cancellation without logging an error
            }
            catch (Exception ex)
            {
                HandleError(ex, "Error processing message.");
            }
        }


        private async Task SendMessage(string message)
        {
            try
            {
                switch (client)
                {
                    case WebSocketClient webSocketClient:
                        await webSocketClient.SendAsync(Encoding.UTF8.GetBytes(message));
                        break;
                    case TcpClient tcpClient:
                        await tcpClient.SendAsync(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                client.SendFailed(new OnSendFailedEventArgs { Data = message, Exception = ex });
                throw;
            }
        }

        private void HandleError(Exception ex, string contextMessage)
        {
            client.SendFailed(new OnSendFailedEventArgs { Data = "", Exception = ex });
            client.Error(new OnErrorEventArgs { Exception = ex });
            logger.LogError($"{contextMessage} Exception: {ex}");
        }
    }
}