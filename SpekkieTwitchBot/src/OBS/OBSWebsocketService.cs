using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SpekkieClassLibrary.OBS.Communication;
using SpekkieClassLibrary.OBS.Enum;
using SpekkieClassLibrary.OBS.Events;
using SpekkieClassLibrary.OBS.Types;
using SpekkieClassLibrary.Twitch.Auth;
using SpekkieTwitchBot.Twitch.FileHandling;
using Logger = SpekkieTwitchBot.General.Logger;

namespace SpekkieTwitchBot.OBS;

public class ObsWebsocketService : IHostedService
{
    private readonly Logger _GeneralLogger;
    private readonly CustomObsWebsocket _Socket;
    private readonly CancellationTokenSource _KeepAliveTokenSource;
    private const int KeepAliveInterval = 500;
    private readonly string _Url;
    private readonly string _Password;

    public ObsWebsocketService(
        Logger generalLogger,
        CustomObsWebsocket socket,
        TwitchFileReader twitchFileReader)
    {
        string jsonData = twitchFileReader.ReadTwitchGeneralAuthFile();
        GeneralTwitchAuth? auth = JsonConvert.DeserializeObject<GeneralTwitchAuth>(jsonData);
        _Url = auth?.ObsUrl ?? "";
        _Password = auth?.Password ?? "";
        _KeepAliveTokenSource = new CancellationTokenSource();
        
        _Socket = socket ?? throw new ArgumentNullException(nameof(socket));

        _Socket.Connected += OnConnect;
        _Socket.Disconnected += OnDisconnect;
        _Socket.StreamStateChanged += OnStreamStateChanged;
        _Socket.RecordStateChanged += OnRecordStateChanged;
        _Socket.VirtualcamStateChanged += OnVirtualCamStateChanged;

        _GeneralLogger = generalLogger;
    }
    
    private void OnConnect(object? sender, EventArgs e)
    {      
        OutputStatus streamStatus = _Socket.GetStreamStatus();
        _GeneralLogger.LogInfo($"Stream active: {streamStatus.IsActive.ToString()}");
        OnStreamStateChanged(_Socket,
            streamStatus.IsActive
                ? new StreamStateChangedEventArgs(new OutputStateChanged
                    { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStarted) })
                : new StreamStateChangedEventArgs(new OutputStateChanged { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStopped) }));

        RecordingStatus recordStatus = _Socket.GetRecordStatus();
        _GeneralLogger.LogInfo($"Recording active: {recordStatus.IsRecording.ToString()}");
        OnRecordStateChanged(_Socket,
            streamStatus.IsActive
                ? new RecordStateChangedEventArgs(new RecordStateChanged { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStarted) })
                : new RecordStateChangedEventArgs(new RecordStateChanged { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStopped) }));

        VirtualCamStatus camStatus = _Socket.GetVirtualCamStatus();
        _GeneralLogger.LogInfo($"Cam status active: {camStatus.IsActive.ToString()}");
        OnVirtualCamStateChanged(_Socket,
            streamStatus.IsActive
                ? new VirtualcamStateChangedEventArgs(new OutputStateChanged { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStarted) })
                : new VirtualcamStateChangedEventArgs(new OutputStateChanged { IsActive = true, StateStr = nameof(OutputState.ObsWebsocketOutputStopped) }));

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

        if(e.ObsCloseCode == ObsCloseCodes.AuthenticationFailed)
        {
            _GeneralLogger.LogError("Authentication Failed");
        }
        else if (e.WebsocketDisconnectionInfo.Exception != null)
            _GeneralLogger.LogError($@"Connection failed: 
                                     CloseCode: {e.ObsCloseCode} 
                                     Desc: {e.WebsocketDisconnectionInfo.CloseStatusDescription} 
                                     Exception:{e.WebsocketDisconnectionInfo.Exception?.Message}\n
                                     Type: {e.WebsocketDisconnectionInfo.Type}");
        else
            _GeneralLogger.LogError($@"Connection failed: 
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
            _ => "State unknown...",
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
            _ => "State unknown...",
        };
        _GeneralLogger.LogInfo($"Recording state changed to: {state}");
    }

    private void OnVirtualCamStateChanged(object? sender, VirtualcamStateChangedEventArgs args)
    {
        string state = args.OutputState.State switch
        {
            OutputState.ObsWebsocketOutputStarting => "VirtualCam starting...",
            OutputState.ObsWebsocketOutputStarted => "VirtualCam Started",
            OutputState.ObsWebsocketOutputStopping => "VirtualCam stopping...",
            OutputState.ObsWebsocketOutputStopped => "VirtualCam Stopped",
            _ => "State unknown",
        };
        _GeneralLogger.LogInfo($"Virtual Cam state changed to: {state}");
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
}