using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SpekkieClassLibrary.OBS.Communication;
using SpekkieClassLibrary.OBS.Enum;
using SpekkieClassLibrary.OBS.Events;
using SpekkieClassLibrary.OBS.Types;
using SpekkieClassLibrary.Twitch.Auth;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using Logger = SpekkieTwitchBot.General.FileHandling.Logger;

namespace SpekkieTwitchBot.OBS.OBSServiceNew;

public class ObsWebsocketService : IHostedService
{
    private const int KeepAliveInterval = 500;
    private readonly Logger _GeneralLogger;
    private readonly CancellationTokenSource _KeepAliveTokenSource;
    private readonly ObsWebSocket _Socket;
    private readonly string? _Url;
    private readonly string? _Password;

    public ObsWebsocketService(
        Logger generalLogger,
        ObsWebSocket socket,
        TwitchFileReader twitchFileReader)
    {
        _GeneralLogger = generalLogger ?? throw new ArgumentNullException(nameof(generalLogger));
        _Socket = socket ?? throw new ArgumentNullException(nameof(socket));

        string jsonData = twitchFileReader.ReadTwitchGeneralAuthFile();
        GeneralTwitchAuth? auth = JsonConvert.DeserializeObject<GeneralTwitchAuth>(jsonData);
        int authCorrect = VerifyAuth(auth);
        switch (authCorrect)
        {
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
        _Url = auth?.ObsUrl;
        _Password = auth?.Password;
        _KeepAliveTokenSource = new CancellationTokenSource();
        
        _Socket.OnConnected += OnConnect;
        _Socket.OnDisconnected += OnDisconnect;
        _Socket.OnStreamStateChanged += OnStreamStateChanged;
        _Socket.OnRecordStateChanged += OnRecordStateChanged;
    }

    private static int VerifyAuth(GeneralTwitchAuth? auth)
    {
        int status = 0;
        if (auth == null)
            status = 1;
        if (string.IsNullOrEmpty(auth?.ObsUrl))
            status = 2;
        if (string.IsNullOrEmpty(auth?.Password))
            status = 3;

        return status;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _Socket.ConnectAsync(_Url, _Password);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
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
        Task.Factory.StartNew(() =>
        {
            while (true)
            {
                Thread.Sleep(KeepAliveInterval);
                if (_KeepAliveTokenSource.IsCancellationRequested)
                    break;
            }
        }, keepAliveToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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