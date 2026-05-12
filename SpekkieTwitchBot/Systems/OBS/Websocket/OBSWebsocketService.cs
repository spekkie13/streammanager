using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.OBS.Communication;
using SpekkieClassLibrary.OBS.Enum;
using SpekkieClassLibrary.OBS.Events;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using Logger = SpekkieTwitchBot.General.FileHandling.Logger;

namespace SpekkieTwitchBot.Systems.OBS.Websocket;

public class ObsWebsocketService : IHostedService
{
    private readonly ITwitchAuthTokenProvider _TwitchAuthTokenProvider;
    private readonly ObsWebSocket _Socket;
    private readonly Logger _GeneralLogger;
    private readonly CancellationTokenSource _KeepAliveTokenSource;
    
    private const int KeepAliveInterval = 500;
    private string? _Url;
    private string? _Password;

    public ObsWebsocketService(
        Logger generalLogger,
        ObsWebSocket socket,
        ITwitchAuthTokenProvider twitchAuthTokenProvider
    ) {
        _GeneralLogger = generalLogger ?? throw new ArgumentNullException(nameof(generalLogger));
        _Socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _TwitchAuthTokenProvider = twitchAuthTokenProvider ?? throw new ArgumentNullException(nameof(twitchAuthTokenProvider));
        _KeepAliveTokenSource = new CancellationTokenSource();
        
        _Socket.OnConnected += OnConnect;
        _Socket.OnDisconnected += OnDisconnect;
        _Socket.OnStreamStateChanged += OnStreamStateChanged;
        _Socket.OnRecordStateChanged += OnRecordStateChanged;
    }

    private void VerifyAuth(TwitchGeneralFile? auth)
    {
        int status = 0;
        if (auth == null)
            status = 1;
        if (string.IsNullOrEmpty(auth?.ObsUrl))
            status = 2;
        if (string.IsNullOrEmpty(auth?.Password))
            status = 3;

        switch (status)
        {
            case 0:
                _Url = auth?.ObsUrl;
                _Password = auth?.Password;
                break;
            case 1:
                _GeneralLogger.LogError("General auth file is empty.");
                throw new ArgumentException("General auth file is empty.");
            case 2:
                _GeneralLogger.LogError("General auth file is missing OBS URL.");
                throw new ArgumentException("General auth file is missing OBS URL.");
            case 3:
                _GeneralLogger.LogError("General auth file is missing password.");
                throw new ArgumentException("General auth file is missing password.");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        TwitchGeneralFile twitchGeneralFile = await _TwitchAuthTokenProvider.ReadIdentityAsync(cancellationToken);
        VerifyAuth(twitchGeneralFile);
        
        _GeneralLogger.LogInfo("[BOOT] OBSWebSocketService starting.");
        _Socket.ConnectAsync(_Url, _Password);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _GeneralLogger.LogInfo("[BOOT] OBSWebSocketService stopping.");
        _Socket.Disconnect();
        return Task.CompletedTask;
    }

    private void OnConnect(object? sender, EventArgs e)
    {
        OutputStatus streamStatus = _Socket.GetStreamStatus();
        _GeneralLogger.LogInfo($"Stream active: {streamStatus.IsActive.ToString()}");
        OnStreamStateChanged(_Socket,
            streamStatus.IsActive
                ? new StreamStateChangedEventArgs(new OutputStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStarted) })
                : new StreamStateChangedEventArgs(new OutputStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStopped) }));

        RecordingStatus recordStatus = _Socket.GetRecordStatus();
        _GeneralLogger.LogInfo($"Recording active: {recordStatus.IsRecording.ToString()}");
        OnRecordStateChanged(_Socket,
            streamStatus.IsActive
                ? new RecordStateChangedEventArgs(new RecordStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStarted) })
                : new RecordStateChangedEventArgs(new RecordStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStopped) }));

        CancellationToken keepAliveToken = _KeepAliveTokenSource.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                while (true)
                    await Task.Delay(KeepAliveInterval, keepAliveToken);
            }
            catch (OperationCanceledException) { }
        });
    }

    private void OnDisconnect(object? sender, ObsDisconnectionInfo e)
    {
        _KeepAliveTokenSource.Cancel();

        if (e.ObsCloseCode == ObsCloseCodes.AuthenticationFailed)
            _GeneralLogger.LogError("Authentication Failed");
        else if (e.WebsocketDisconnectionInfo.Exception != null)
            _GeneralLogger.LogWarning($@"Connection failed: 
                                     CloseCode: {e.ObsCloseCode} 
                                     Desc: {e.WebsocketDisconnectionInfo.CloseStatusDescription} 
                                     Exception:{e.WebsocketDisconnectionInfo.Exception?.Message}\n
                                     Type: {e.WebsocketDisconnectionInfo.Type}");
        else
            _GeneralLogger.LogWarning($@"Connection failed: 
                                     CloseCode: {e.ObsCloseCode} 
                                     Desc: {e.WebsocketDisconnectionInfo.CloseStatusDescription} 
                                     Exception:{e.WebsocketDisconnectionInfo.Exception?.Message}\n
                                     Type: {e.WebsocketDisconnectionInfo.Type}");
    }

    private void OnStreamStateChanged(object? sender, StreamStateChangedEventArgs args)
    {
        string state = args.OutputState.State switch
        {
            OutputState.ObsWebsocketOutputStarting => "Stream starting...",
            OutputState.ObsWebsocketOutputStarted => "Stream started...",
            OutputState.ObsWebsocketOutputStopping => "Stream stopping...",
            OutputState.ObsWebsocketOutputStopped => "Stream stopped...",
            _ => "State unknown..."
        };
        _GeneralLogger.LogInfo($"Stream state changed to: {state}");
    }

    private void OnRecordStateChanged(object? sender, RecordStateChangedEventArgs args)
    {
        string state = args.OutputState.State switch
        {
            OutputState.ObsWebsocketOutputStarting => "Recording starting...",
            OutputState.ObsWebsocketOutputStarted or OutputState.ObsWebsocketOutputResumed => "Recording started...",
            OutputState.ObsWebsocketOutputStopping => "Recording stopping...",
            OutputState.ObsWebsocketOutputStopped => "Recording stopped...",
            OutputState.ObsWebsocketOutputPaused => "Recording paused...",
            _ => "State unknown..."
        };
        _GeneralLogger.LogInfo($"Recording state changed to: {state}");
    }
}