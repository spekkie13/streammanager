using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;
using SpekkieTwitchBot.FileHandling.Twitch;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models.Twitch;

namespace SpekkieTwitchBot.Web;

public class ObsWebsocketService : IHostedService
{
    private readonly IConfiguration _Configuration;
    private readonly ILogger<ObsWebsocketService> _Logger;
    private readonly Logger _GeneralLogger;
    private readonly OBSWebsocket _Socket;
    private readonly CancellationTokenSource _KeepAliveTokenSource;
    private const int KeepAliveInterval = 500;
    private readonly string _Url;
    private readonly string _Password;

    public ObsWebsocketService(
        IConfiguration configuration, 
        ILogger<ObsWebsocketService> logger, 
        Logger generalLogger,
        OBSWebsocket socket,
        TwitchFileReader twitchFileReader)
    {
        string jsonData = twitchFileReader.ReadTwitchAuthFile();
        TwitchAuth? auth = JsonConvert.DeserializeObject<TwitchAuth>(jsonData);
        _Url = auth?.Obs_Url ?? "";
        _Password = auth?.Password ?? "";
        _KeepAliveTokenSource = new CancellationTokenSource();
        
        _Configuration = configuration;
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        OnStreamStateChanged(_Socket,
            streamStatus.IsActive
                ? new StreamStateChangedEventArgs(new OutputStateChanged()
                    { IsActive = true, StateStr = nameof(OutputState.OBS_WEBSOCKET_OUTPUT_STARTED) })
                : new StreamStateChangedEventArgs(new OutputStateChanged()
                    { IsActive = true, StateStr = nameof(OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED) }));

        RecordingStatus recordStatus = _Socket.GetRecordStatus();
        OnRecordStateChanged(_Socket,
            streamStatus.IsActive
                ? new RecordStateChangedEventArgs(new RecordStateChanged()
                    { IsActive = true, StateStr = nameof(OutputState.OBS_WEBSOCKET_OUTPUT_STARTED) })
                : new RecordStateChangedEventArgs(new RecordStateChanged()
                    { IsActive = true, StateStr = nameof(OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED) }));

        VirtualCamStatus camStatus = _Socket.GetVirtualCamStatus();
        OnVirtualCamStateChanged(_Socket,
            streamStatus.IsActive
                ? new VirtualcamStateChangedEventArgs(new OutputStateChanged()
                    { IsActive = true, StateStr = nameof(OutputState.OBS_WEBSOCKET_OUTPUT_STARTED) })
                : new VirtualcamStateChangedEventArgs(new OutputStateChanged()
                    { IsActive = true, StateStr = nameof(OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED) }));

        CancellationToken keepAliveToken = _KeepAliveTokenSource.Token;
        Task statPollKeepAlive = Task.Factory.StartNew(() =>
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
        else if(e.WebsocketDisconnectionInfo != null)
            if (e.WebsocketDisconnectionInfo.Exception != null)
                _GeneralLogger.LogError($@"Connection failed: 
                                     CloseCode: {e.ObsCloseCode} 
                                     Desc: {e.WebsocketDisconnectionInfo?.CloseStatusDescription} 
                                     Exception:{e.WebsocketDisconnectionInfo?.Exception?.Message}\n
                                     Type: {e.WebsocketDisconnectionInfo?.Type}");
            else
                _GeneralLogger.LogError($@"Connection failed: 
                                     CloseCode: {e.ObsCloseCode} 
                                     Desc: {e.WebsocketDisconnectionInfo?.CloseStatusDescription} 
                                     Exception:{e.WebsocketDisconnectionInfo?.Exception?.Message}\n
                                     Type: {e.WebsocketDisconnectionInfo?.Type}");
        else
        {
            _GeneralLogger.LogError($"Connection failed: CloseCode: {e.ObsCloseCode}");
        }
    }

    private void OnStreamStateChanged(object? sender, StreamStateChangedEventArgs args)
    {
        string state = args.OutputState.State switch
        {
            OutputState.OBS_WEBSOCKET_OUTPUT_STARTING => "Stream starting...",
            OutputState.OBS_WEBSOCKET_OUTPUT_STARTED => "Stream started...",
            OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING => "Stream stopping...",
            OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED => "Stream stopped...",
            _ => "State unknown...",
        };
        _GeneralLogger.LogInfo($"Stream state changed to: {state}");
    }

    private void OnRecordStateChanged(object? sender, RecordStateChangedEventArgs args)
    {
        string state = args.OutputState.State switch
        {
            OutputState.OBS_WEBSOCKET_OUTPUT_STARTING => "Recording starting...",
            OutputState.OBS_WEBSOCKET_OUTPUT_STARTED or OutputState.OBS_WEBSOCKET_OUTPUT_RESUMED => "Recording started...",
            OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING => "Recording stopping...",
            OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED => "Recording stopped...",
            OutputState.OBS_WEBSOCKET_OUTPUT_PAUSED => "Recording paused...",
            _ => "State unknown...",
        };
        _GeneralLogger.LogInfo($"Recording state changed to: {state}");
    }

    private void OnVirtualCamStateChanged(object? sender, VirtualcamStateChangedEventArgs args)
    {
        string state = args.OutputState.State switch
        {
            OutputState.OBS_WEBSOCKET_OUTPUT_STARTING => "VirtualCam starting...",
            OutputState.OBS_WEBSOCKET_OUTPUT_STARTED => "VirtualCam Started",
            OutputState.OBS_WEBSOCKET_OUTPUT_STOPPING => "VirtualCam stopping...",
            OutputState.OBS_WEBSOCKET_OUTPUT_STOPPED => "VirtualCam Stopped",
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