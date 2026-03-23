using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.OBS.Communication;
using SpekkieClassLibrary.OBS.Enum;
using SpekkieClassLibrary.OBS.Events;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using Logger = SpekkieTwitchBot.General.FileHandling.Logger;

namespace SpekkieTwitchBot.Systems.OBS;

public class ObsWebsocketService : IHostedService
{
    private readonly ITwitchAuthTokenProvider _twitchAuthTokenProvider;
    private readonly ObsWebSocket _socket;
    private readonly Logger _generalLogger;
    private readonly CancellationTokenSource _keepAliveTokenSource;
    
    private const int KeepAliveInterval = 500;
    private string? _url;
    private string? _password;

    public ObsWebsocketService(
        Logger generalLogger,
        ObsWebSocket socket,
        ITwitchAuthTokenProvider twitchAuthTokenProvider
    ) {
        _generalLogger = generalLogger ?? throw new ArgumentNullException(nameof(generalLogger));
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _twitchAuthTokenProvider = twitchAuthTokenProvider ?? throw new ArgumentNullException(nameof(twitchAuthTokenProvider));
        _keepAliveTokenSource = new CancellationTokenSource();
        
        _socket.OnConnected += OnConnect;
        _socket.OnDisconnected += OnDisconnect;
        _socket.OnStreamStateChanged += OnStreamStateChanged;
        _socket.OnRecordStateChanged += OnRecordStateChanged;
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
                _url = auth?.ObsUrl;
                _password = auth?.Password;
                break;
            case 1:
                _generalLogger.LogError("General auth file is empty.");
                throw new ArgumentException("General auth file is empty.");
            case 2:
                _generalLogger.LogError("General auth file is missing OBS URL.");
                throw new ArgumentException("General auth file is missing OBS URL.");
            case 3:
                _generalLogger.LogError("General auth file is missing password.");
                throw new ArgumentException("General auth file is missing password.");
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        TwitchGeneralFile twitchGeneralFile = await _twitchAuthTokenProvider.ReadIdentityAsync(cancellationToken);
        VerifyAuth(twitchGeneralFile);
        
        _generalLogger.LogInfo("[BOOT] OBSWebSocketService starting.");
        _socket.ConnectAsync(_url, _password);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _generalLogger.LogInfo("[BOOT] OBSWebSocketService stopping.");
        _socket.Disconnect();
        return Task.CompletedTask;
    }

    private void OnConnect(object? sender, EventArgs e)
    {
        OutputStatus streamStatus = _socket.GetStreamStatus();
        _generalLogger.LogInfo($"Stream active: {streamStatus.IsActive.ToString()}");
        OnStreamStateChanged(_socket,
            streamStatus.IsActive
                ? new StreamStateChangedEventArgs(new OutputStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStarted) })
                : new StreamStateChangedEventArgs(new OutputStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStopped) }));

        RecordingStatus recordStatus = _socket.GetRecordStatus();
        _generalLogger.LogInfo($"Recording active: {recordStatus.IsRecording.ToString()}");
        OnRecordStateChanged(_socket,
            streamStatus.IsActive
                ? new RecordStateChangedEventArgs(new RecordStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStarted) })
                : new RecordStateChangedEventArgs(new RecordStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStopped) }));

        CancellationToken keepAliveToken = _keepAliveTokenSource.Token;
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
        _keepAliveTokenSource.Cancel();

        if (e.ObsCloseCode == ObsCloseCodes.AuthenticationFailed)
            _generalLogger.LogError("Authentication Failed");
        else if (e.WebsocketDisconnectionInfo.Exception != null)
            _generalLogger.LogWarning($@"Connection failed: 
                                     CloseCode: {e.ObsCloseCode} 
                                     Desc: {e.WebsocketDisconnectionInfo.CloseStatusDescription} 
                                     Exception:{e.WebsocketDisconnectionInfo.Exception?.Message}\n
                                     Type: {e.WebsocketDisconnectionInfo.Type}");
        else
            _generalLogger.LogWarning($@"Connection failed: 
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
        _generalLogger.LogInfo($"Stream state changed to: {state}");
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
        _generalLogger.LogInfo($"Recording state changed to: {state}");
    }
}