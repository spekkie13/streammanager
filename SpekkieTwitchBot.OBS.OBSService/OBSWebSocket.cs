using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Communication;
using SpekkieClassLibrary.OBS.Enum;
using SpekkieClassLibrary.OBS.Events;
using SpekkieClassLibrary.OBS.Types;
using SpekkieTwitchBot.General.FileHandling;
using Websocket.Client;
using Monitor = SpekkieClassLibrary.OBS.Types.Monitor;

#nullable disable
namespace SpekkieTwitchBot.OBS.OBSServiceNew;

public class ObsWebSocket
{
    private const string WebsocketUrlPrefix = "ws://";
    private const int SupportedRpcVersion = 1;
    private TimeSpan _wsTimeout = TimeSpan.FromSeconds(10);
    private string _connectionPassword;
    private WebsocketClient _wsConnection;
    private readonly Logger _logger;

    private delegate void RequestCallback(ObsWebSocket sender, JObject body);

    private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> _responseHandlers = new();
    private static readonly Random Random = new ();

    public ObsWebSocket(WebsocketClient wsConnection, Logger logger)
    {
        _logger = logger;
        _wsConnection = wsConnection;
        _responseHandlers = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();
    }
    
    public TimeSpan WsTimeout
    {
        get => _wsConnection.ReconnectTimeout ?? _wsTimeout;
        set
        {
            _wsTimeout = value;

            _wsConnection.ReconnectTimeout = _wsTimeout;
        }
    }

    public bool IsConnected => _wsConnection.IsRunning;

    public void ConnectAsync(string url, string password)
    {
        if (!url.StartsWith(WebsocketUrlPrefix, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new ArgumentException($"Invalid url, must start with '{WebsocketUrlPrefix}'");
        }

        if (_wsConnection is { IsRunning: true })
        {
            Disconnect();
        }

        _wsConnection = new WebsocketClient(new Uri(url));
        _wsConnection.IsReconnectionEnabled = false;
        _wsConnection.ReconnectTimeout = null;
        _wsConnection.ErrorReconnectTimeout = null;
        _wsConnection.MessageReceived.Subscribe(m => Task.Run(() => WebsocketMessageHandler(this, m)));
        _wsConnection.DisconnectionHappened.Subscribe(d => Task.Run(() => OnWebsocketDisconnect(this, d)));

        _connectionPassword = password;
        _wsConnection.StartOrFail();
    }

    public void Disconnect()
    {
        _connectionPassword = null;
        if (_wsConnection != null)
        {
            // Attempt to both close and dispose the existing connection
            try
            {
                _wsConnection.Stop(WebSocketCloseStatus.NormalClosure, "User requested disconnect");
                ((IDisposable)_wsConnection).Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while disconnecting websocket: {ex.Message}");
            }
            _wsConnection = null;
        }

        KeyValuePair<string, TaskCompletionSource<JObject>>[] unusedHandlers = _responseHandlers.ToArray();
        _responseHandlers.Clear();
        foreach (KeyValuePair<string, TaskCompletionSource<JObject>> cb in unusedHandlers)
        {
            TaskCompletionSource<JObject> tcs = cb.Value;
            tcs.TrySetCanceled();
        }
    }

    private void OnWebsocketDisconnect(object sender, DisconnectionInfo d)
    {
        OnDisconnected?.Invoke(sender,
            d.CloseStatus == null
                ? new ObsDisconnectionInfo(ObsCloseCodes.UnknownReason, "Unknown", d)
                : new ObsDisconnectionInfo((ObsCloseCodes)d.CloseStatus, d.CloseStatusDescription, d));
    }

    private void WebsocketMessageHandler(object sender, ResponseMessage e)
    {
        if (e.MessageType != WebSocketMessageType.Text)
        {
            return;
        }

        ServerMessage msg = JsonConvert.DeserializeObject<ServerMessage>(e.Text) ?? new ServerMessage();
        JObject body = msg.Data;

        switch (msg.OperationCode)
        {
            case MessageTypes.Hello:
                HandleHello(body);
                break;
            case MessageTypes.Identified:
                Task.Run(() => OnConnected?.Invoke(this, EventArgs.Empty));
                break;
            case MessageTypes.RequestResponse:
            case MessageTypes.RequestBatchResponse:
                if (body.TryGetValue("requestId", out JToken value))
                {
                    string msgId = (string)value ?? "";

                    if (_responseHandlers.TryRemove(msgId, out TaskCompletionSource<JObject> handler))
                    {
                        handler.SetResult(body);
                    }
                }

                break;
            case MessageTypes.Event:
                string eventType = body["eventType"]?.ToString();
                Task.Run(() => { ProcessEventType(eventType, body); });
                break;
        }
    }

    private JObject SendRequest(string requestType, JObject additionalFields = null)
    {
        return SendRequest(MessageTypes.Request, requestType, additionalFields);
    }

    private JObject SendRequest(MessageTypes operationCode, string requestType, JObject additionalFields,
        bool waitForReply = true)
    {
        if (_wsConnection == null)
        {
            throw new NullReferenceException("Websocket is not initialized");
        }

        TaskCompletionSource<JObject> tcs = new TaskCompletionSource<JObject>();
        JObject message;
        do
        {
            message = MessageFactory.BuildMessage(operationCode, requestType, additionalFields, out string messageId);
            if (!waitForReply || _responseHandlers.TryAdd(messageId, tcs))
            {
                break;
            }
        } while (true);

        _wsConnection.Send(message.ToString());
        if (!waitForReply) return null;

        tcs.Task.Wait(_wsTimeout.Milliseconds);

        if (tcs.Task.IsCanceled)
            throw new ErrorResponseException("Request canceled", 0);

        JObject result = tcs.Task.Result;
        JToken requestStatus = result["requestStatus"] ?? new JObject();
        bool reqStatus = Convert.ToBoolean(requestStatus["result"]);
        if (!reqStatus)
        {
            JObject status = (JObject)result["requestStatus"];
            string code = result["code"]?.ToString() ?? "";
            throw new ErrorResponseException(
                $"ErrorCode: {code}{(status != null && status.TryGetValue("comment", out JToken s) ? $", Comment: {s}" : "")}",
                Convert.ToInt32(code));
        }

        if (!result.ContainsKey("responseData")) return new JObject();
        JObject responseData = result["responseData"]?.ToObject<JObject>() ?? new JObject();
        return responseData;
    }

    public ObsAuthInfo GetAuthInfo()
    {
        JObject response = SendRequest("GetAuthRequired");
        return new ObsAuthInfo(response);
    }

    public event EventHandler<ProgramSceneChangedEventArgs> OnCurrentProgramSceneChanged;
    public event EventHandler<SceneListChangedEventArgs> OnSceneListChanged;
    public event EventHandler<SceneItemListReindexedEventArgs> OnSceneItemListReindexed;
    public event EventHandler<SceneItemCreatedEventArgs> OnSceneItemCreated;
    public event EventHandler<SceneItemRemovedEventArgs> OnSceneItemRemoved;
    public event EventHandler<SceneItemEnableStateChangedEventArgs> OnSceneItemEnableStateChanged;
    public event EventHandler<SceneItemLockStateChangedEventArgs> OnSceneItemLockStateChanged;
    public event EventHandler<CurrentSceneCollectionChangedEventArgs> OnCurrentSceneCollectionChanged;
    public event EventHandler<SceneCollectionListChangedEventArgs> OnSceneCollectionListChanged;
    public event EventHandler<CurrentSceneTransitionChangedEventArgs> OnCurrentSceneTransitionChanged;
    public event EventHandler<CurrentSceneTransitionDurationChangedEventArgs> OnCurrentSceneTransitionDurationChanged;
    public event EventHandler<SceneTransitionStartedEventArgs> OnSceneTransitionStarted;
    public event EventHandler<SceneTransitionEndedEventArgs> OnSceneTransitionEnded;
    public event EventHandler<SceneTransitionVideoEndedEventArgs> OnSceneTransitionVideoEnded;
    public event EventHandler<CurrentProfileChangedEventArgs> OnCurrentProfileChanged;
    public event EventHandler<ProfileListChangedEventArgs> OnProfileListChanged;
    public event EventHandler<StreamStateChangedEventArgs> OnStreamStateChanged;
    public event EventHandler<RecordStateChangedEventArgs> OnRecordStateChanged;
    public event EventHandler<ReplayBufferStateChangedEventArgs> OnReplayBufferStateChanged;
    public event EventHandler<CurrentPreviewSceneChangedEventArgs> OnCurrentPreviewSceneChanged;
    public event EventHandler<StudioModeStateChangedEventArgs> OnStudioModeStateChanged;
    public event EventHandler OnExitStarted;
    public event EventHandler OnConnected;
    public event EventHandler<ObsDisconnectionInfo> OnDisconnected;
    public event EventHandler<SceneItemSelectedEventArgs> OnSceneItemSelected;
    public event EventHandler<SceneItemTransformEventArgs> OnSceneItemTransformChanged;
    public event EventHandler<InputAudioSyncOffsetChangedEventArgs> OnInputAudioSyncOffsetChanged;
    public event EventHandler<SourceFilterCreatedEventArgs> OnSourceFilterCreated;
    public event EventHandler<SourceFilterRemovedEventArgs> OnSourceFilterRemoved;
    public event EventHandler<SourceFilterListReindexedEventArgs> OnSourceFilterListReindexed;
    public event EventHandler<SourceFilterEnableStateChangedEventArgs> OnSourceFilterEnableStateChanged;
    public event EventHandler<InputMuteStateChangedEventArgs> OnInputMuteStateChanged;
    public event EventHandler<InputVolumeChangedEventArgs> OnInputVolumeChanged;
    public event EventHandler<VendorEventArgs> OnVendorEvent;
    public event EventHandler<MediaInputPlaybackEndedEventArgs> OnMediaInputPlaybackEnded;
    public event EventHandler<MediaInputPlaybackStartedEventArgs> OnMediaInputPlaybackStarted;
    public event EventHandler<MediaInputActionTriggeredEventArgs> OnMediaInputActionTriggered;
    public event EventHandler<VirtualcamStateChangedEventArgs> OnVirtualcamStateChanged;
    public event EventHandler<CurrentSceneCollectionChangingEventArgs> OnCurrentSceneCollectionChanging;
    public event EventHandler<CurrentProfileChangingEventArgs> OnCurrentProfileChanging;
    public event EventHandler<SourceFilterNameChangedEventArgs> OnSourceFilterNameChanged;
    public event EventHandler<InputCreatedEventArgs> OnInputCreated;
    public event EventHandler<InputRemovedEventArgs> OnInputRemoved;
    public event EventHandler<InputNameChangedEventArgs> OnInputNameChanged;
    public event EventHandler<InputActiveStateChangedEventArgs> OnInputActiveStateChanged;
    public event EventHandler<InputShowStateChangedEventArgs> OnInputShowStateChanged;
    public event EventHandler<InputAudioBalanceChangedEventArgs> OnInputAudioBalanceChanged;
    public event EventHandler<InputAudioTracksChangedEventArgs> OnInputAudioTracksChanged;
    public event EventHandler<InputAudioMonitorTypeChangedEventArgs> OnInputAudioMonitorTypeChanged;
    public event EventHandler<InputVolumeMetersEventArgs> OnInputVolumeMeters;
    public event EventHandler<ReplayBufferSavedEventArgs> OnReplayBufferSaved;
    public event EventHandler<SceneCreatedEventArgs> OnSceneCreated;
    public event EventHandler<SceneRemovedEventArgs> OnSceneRemoved;
    public event EventHandler<SceneNameChangedEventArgs> OnSceneNameChanged;

    private void SendIdentify(string password, ObsAuthInfo authInfo)
    {
        JObject requestFields = new JObject
        {
            { "rpcVersion", SupportedRpcVersion }
        };

        if (authInfo != null)
        {
            string secret = HashEncode(password + authInfo.PasswordSalt);
            string authResponse = HashEncode(secret + authInfo.Challenge);
            requestFields.Add("authentication", authResponse);
        }

        SendRequest(MessageTypes.Identify, "", requestFields, false);
    }

    private static string HashEncode(string input)
    {
        using SHA256 sha256 = SHA256.Create();

        byte[] textBytes = Encoding.ASCII.GetBytes(input);
        byte[] hash = sha256.ComputeHash(textBytes);

        return Convert.ToBase64String(hash);
    }

    protected static string NewMessageId(int length = 16)
    {
        const string Pool = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        string result = "";
        for (int i = 0; i < length; i++)
        {
            int index = Random.Next(0, Pool.Length - 1);
            result += Pool[index];
        }

        return result;
    }

    private void HandleHello(JObject payload)
    {
        if (!_wsConnection.IsStarted) return;

        ObsAuthInfo authInfo = new ObsAuthInfo();
        if (payload.TryGetValue("authentication", out JToken value))
            authInfo = new ObsAuthInfo((JObject)value);
        
        SendIdentify(_connectionPassword, authInfo);

        _connectionPassword = "";
    }

    private const string RequestFieldVolumeDb = "inputVolumeDb";
    private const string RequestFieldVolumeMul = "inputVolumeMul";
    private const string ResponseFieldImageData = "imageData";

    public ObsVideoSettings GetVideoSettings()
    {
        JObject response = SendRequest(nameof(GetVideoSettings));
        ObsVideoSettings settings = JsonConvert.DeserializeObject<ObsVideoSettings>(response.ToString()) ?? new ObsVideoSettings();
        return settings;
    }

    public string SaveSourceScreenshot(string sourceName, string imageFormat, string imageFilePath, int imageWidth = -1,
        int imageHeight = -1, int imageCompressionQuality = -1)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(imageFormat), imageFormat },
            { nameof(imageFilePath), imageFilePath }
        };

        if (imageWidth > -1)
            request.Add(nameof(imageWidth), imageWidth);
        
        if (imageHeight > -1)
            request.Add(nameof(imageHeight), imageHeight);

        if (imageCompressionQuality > -1)
            request.Add(nameof(imageCompressionQuality), imageCompressionQuality);

        JObject response = SendRequest(nameof(SaveSourceScreenshot), request);
        string imageData = response["imageData"]?.ToString() ?? "";
        return imageData;
    }

    public string SaveSourceScreenshot(string sourceName, string imageFormat, string imageFilePath) 
    {
        return SaveSourceScreenshot(sourceName, imageFormat, imageFilePath, -1);
    }

    public void TriggerHotkeyByName(string hotkeyName)
    {
        JObject request = new JObject
        {
            { nameof(hotkeyName), hotkeyName }
        };

        SendRequest(nameof(TriggerHotkeyByName), request);
    }

    public string GetCurrentProgramScene()
    {
        JObject response = SendRequest(nameof(GetCurrentProgramScene));
        string currentProgramSceneName = response["currentProgramSceneName"]?.ToString() ?? "";
        return currentProgramSceneName;
    }

    public void SetCurrentProgramScene(string sceneName)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        SendRequest(nameof(SetCurrentProgramScene), request);
    }

    public ObsStats GetStats()
    {
        JObject response = SendRequest(nameof(GetStats));
        ObsStats stats = JsonConvert.DeserializeObject<ObsStats>(response.ToString()) ?? new ObsStats();
        return stats;
    }

    public List<SceneBasicInfo> ListScenes()
    {
        GetSceneListInfo response = GetSceneList();
        List<SceneBasicInfo> info = response.Scenes ?? new List<SceneBasicInfo>();
        return info;
    }

    public GetSceneListInfo GetSceneList()
    {
        JObject response = SendRequest(nameof(GetSceneList));
        GetSceneListInfo info = JsonConvert.DeserializeObject<GetSceneListInfo>(response.ToString()) ??
                                new GetSceneListInfo();
        return info;
    }

    public TransitionOverrideInfo GetSceneSceneTransitionOverride(string sceneName)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        JObject response = SendRequest(nameof(GetSceneSceneTransitionOverride), request);
        TransitionOverrideInfo info = response.ToObject<TransitionOverrideInfo>() ?? new TransitionOverrideInfo();
        return info;
    }

    public void SetSceneSceneTransitionOverride(string sceneName, string transitionName, int transitionDuration = -1)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(transitionName), transitionName }
        };

        if (transitionDuration >= 0)
        {
            request.Add(nameof(transitionDuration), transitionDuration);
        }

        SendRequest(nameof(SetSceneSceneTransitionOverride), request);
    }

    public void SetTBarPosition(double position, bool release = true)
    {
        if (position is < 0.0 or > 1.0) throw new ArgumentOutOfRangeException(nameof(position));
        
        JObject request = new JObject
        {
            { nameof(position), position },
            { nameof(release), release }
        };

        SendRequest(nameof(SetTBarPosition), request);
    }

    public void SetSourceFilterSettings(string sourceName, string filterName, JObject filterSettings, bool overlay = false)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(filterSettings), filterSettings },
            { nameof(overlay), overlay }
        };

        SendRequest(nameof(SetSourceFilterSettings), request);
    }

    public void SetSourceFilterSettings(string sourceName, string filterName, FilterSettings filterSettings, bool overlay = false)
    {
        SetSourceFilterSettings(sourceName, filterName, JObject.FromObject(filterSettings), overlay);
    }

    public void SetSourceFilterEnabled(string sourceName, string filterName, bool filterEnabled)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(filterEnabled), filterEnabled }
        };

        SendRequest(nameof(SetSourceFilterEnabled), request);
    }

    public List<FilterSettings> GetSourceFilterList(string sourceName)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName }
        };

        JObject response = SendRequest(nameof(GetSourceFilterList), request);
        if (!response.HasValues)
        {
            return new List<FilterSettings>();
        }

        string filter = response["filters"]?.ToString() ?? "";
        List<FilterSettings> settings = JsonConvert.DeserializeObject<List<FilterSettings>>(filter) ?? new List<FilterSettings>();
        return settings;
    }

    public FilterSettings GetSourceFilter(string sourceName, string filterName)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName }
        };

        JObject response = SendRequest(nameof(GetSourceFilter), request);
        FilterSettings settings = JsonConvert.DeserializeObject<FilterSettings>(response.ToString()) ?? new FilterSettings();
        return settings;
    }

    public bool RemoveSourceFilter(string sourceName, string filterName)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName }
        };
        try
        {
            SendRequest(nameof(RemoveSourceFilter), request);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }

        return false;
    }

    public void CreateSourceFilter(string sourceName, string filterName, string filterKind, JObject filterSettings)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(filterKind), filterKind },
            { nameof(filterSettings), filterSettings }
        };

        SendRequest(nameof(CreateSourceFilter), request);
    }

    public void CreateSourceFilter(string sourceName, string filterName, string filterKind,
        FilterSettings filterSettings)
    {
        CreateSourceFilter(sourceName, filterName, filterKind, JObject.FromObject(filterSettings));
    }

    public bool ToggleStream()
    {
        JObject response = SendRequest(nameof(ToggleStream));
        bool outputActive = Convert.ToBoolean(response["outputActive"]);
        return outputActive;
    }

    public void ToggleRecord()
    {
        SendRequest(nameof(ToggleRecord));
    }

    public OutputStatus GetStreamStatus()
    {
        JObject response = SendRequest(nameof(GetStreamStatus));
        OutputStatus outputStatus = new OutputStatus(response);
        return outputStatus;
    }

    public TransitionSettings GetCurrentSceneTransition()
    {
        JObject response = SendRequest(nameof(GetCurrentSceneTransition));
        return new TransitionSettings(response);
    }

    public void SetCurrentSceneTransition(string transitionName)
    {
        JObject request = new JObject
        {
            { nameof(transitionName), transitionName }
        };

        SendRequest(nameof(SetCurrentSceneTransition), request);
    }

    public void SetCurrentSceneTransitionDuration(int transitionDuration)
    {
        JObject request = new JObject
        {
            { nameof(transitionDuration), transitionDuration }
        };

        SendRequest(nameof(SetCurrentSceneTransitionDuration), request);
    }

    public void SetCurrentSceneTransitionSettings(JObject transitionSettings, bool overlay)
    {
        JObject requestFields = new JObject
        {
            { nameof(transitionSettings), JToken.FromObject(transitionSettings) },
            { nameof(overlay), overlay }
        };

        SendRequest(nameof(SetCurrentSceneTransitionSettings), requestFields);
    }

    public void SetInputVolume(string inputName, float inputVolume, bool inputVolumeDb = false)
    {
        JObject requestFields = new JObject
        {
            { nameof(inputName), inputName },
            { inputVolumeDb ? RequestFieldVolumeDb : RequestFieldVolumeMul, inputVolume }
        };

        SendRequest(nameof(SetInputVolume), requestFields);
    }

    public VolumeInfo GetInputVolume(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        JObject response = SendRequest(nameof(GetInputVolume), request);
        return new VolumeInfo(response);
    }

    public bool GetInputMute(string inputName)
    {
        JObject requestFields = new JObject
        {
            { nameof(inputName), inputName }
        };

        JObject response = SendRequest(nameof(GetInputMute), requestFields);
        bool inputMuted = Convert.ToBoolean(response["inputMuted"]);
        return inputMuted;
    }

    public void SetInputMute(string inputName, bool inputMuted)
    {
        JObject requestFields = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputMuted), inputMuted }
        };

        SendRequest(nameof(SetInputMute), requestFields);
    }

    public void ToggleInputMute(string inputName)
    {
        JObject requestFields = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(ToggleInputMute), requestFields);
    }

    public void SetSceneItemTransform(string sceneName, int sceneItemId, JObject sceneItemTransform)
    {
        JObject requestFields = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemTransform), sceneItemTransform }
        };

        SendRequest(nameof(SetSceneItemTransform), requestFields);
    }

    public void SetSceneItemTransform(string sceneName, int sceneItemId, SceneItemTransformInfo sceneItemTransform)
    {
        SetSceneItemTransform(sceneName, sceneItemId, JObject.FromObject(sceneItemTransform));
    }

    public void SetCurrentSceneCollection(string sceneCollectionName)
    {
        JObject requestFields = new JObject
        {
            { nameof(sceneCollectionName), sceneCollectionName }
        };

        SendRequest(nameof(SetCurrentSceneCollection), requestFields);
    }

    public string GetCurrentSceneCollection()
    {
        JObject response = SendRequest(nameof(GetSceneCollectionList));
        JToken currentCollectionName = response["currentSceneCollectionName"];
        string name = currentCollectionName?.ToString() ?? "";
        return name;
    }

    public List<string> GetSceneCollectionList()
    {
        JObject response = SendRequest(nameof(GetSceneCollectionList));
        string collection = response["sceneCollections"]?.ToString() ?? "";
        List<string> sceneCollections = JsonConvert.DeserializeObject<List<string>>(collection) ?? new List<string>();
        return sceneCollections;
    }

    public void SetCurrentProfile(string profileName)
    {
        JObject requestFields = new JObject
        {
            { nameof(profileName), profileName }
        };

        SendRequest(nameof(SetCurrentProfile), requestFields);
    }

    public GetProfileListInfo GetProfileList()
    {
        JObject response = SendRequest(nameof(GetProfileList));
        GetProfileListInfo info = JsonConvert.DeserializeObject<GetProfileListInfo>(response.ToString()) ?? new GetProfileListInfo();
        return info;
    }

    public void StartStream()
    {
        SendRequest(nameof(StartStream));
    }

    public void StopStream()
    {
        SendRequest(nameof(StopStream));
    }

    public void StartRecord()
    {
        SendRequest(nameof(StartRecord));
    }

    public string StopRecord()
    {
        JObject response = SendRequest(nameof(StopRecord));
        string outputPath = response["outputPath"]?.ToString() ?? "";
        return outputPath;
    }

    public void PauseRecord()
    {
        SendRequest(nameof(PauseRecord));
    }

    public void ResumeRecord()
    {
        SendRequest(nameof(ResumeRecord));
    }

    public string GetRecordDirectory()
    {
        JObject response = SendRequest(nameof(GetRecordDirectory));
        string recordDirectory = response["recordDirectory"]?.ToString() ?? "";
        return recordDirectory;
    }

    public RecordingStatus GetRecordStatus()
    {
        JObject response = SendRequest(nameof(GetRecordStatus));
        RecordingStatus status = JsonConvert.DeserializeObject<RecordingStatus>(response.ToString()) ??
                                 new RecordingStatus();
        return status;
    }

    public bool GetReplayBufferStatus()
    {
        JObject response = SendRequest(nameof(GetReplayBufferStatus));
        bool outputActive = Convert.ToBoolean(response["outputActive"]);
        return outputActive;
    }

    public GetTransitionListInfo GetSceneTransitionList()
    {
        JObject response = SendRequest(nameof(GetSceneTransitionList));
        GetTransitionListInfo info = JsonConvert.DeserializeObject<GetTransitionListInfo>(response.ToString()) ?? new GetTransitionListInfo();
        return info;
    }

    public bool GetStudioModeEnabled()
    {
        JObject response = SendRequest(nameof(GetStudioModeEnabled));
        bool studioModeEnabled = Convert.ToBoolean(response["studioModeEnabled"]);
        return studioModeEnabled;
    }

    public void SetStudioModeEnabled(bool studioModeEnabled)
    {
        JObject requestFields = new JObject
        {
            { nameof(studioModeEnabled), studioModeEnabled }
        };

        SendRequest(nameof(SetStudioModeEnabled), requestFields);
    }

    public string GetCurrentPreviewScene()
    {
        JObject response = SendRequest(nameof(GetCurrentPreviewScene));
        string currentPreviewSceneName = response["currentPreviewSceneName"]?.ToString() ?? "";
        return currentPreviewSceneName;
    }

    public void SetCurrentPreviewScene(string sceneName)
    {
        JObject requestFields = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        SendRequest(nameof(SetCurrentPreviewScene), requestFields);
    }

    public void TriggerStudioModeTransition()
    {
        SendRequest(nameof(TriggerStudioModeTransition));
    }

    public void ToggleReplayBuffer()
    {
        SendRequest(nameof(ToggleReplayBuffer));
    }

    public void StartReplayBuffer()
    {
        SendRequest(nameof(StartReplayBuffer));
    }

    public void StopReplayBuffer()
    {
        SendRequest(nameof(StopReplayBuffer));
    }

    public void SaveReplayBuffer()
    {
        SendRequest(nameof(SaveReplayBuffer));
    }

    public void SetInputAudioSyncOffset(string inputName, int inputAudioSyncOffset)
    {
        JObject requestFields = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputAudioSyncOffset), inputAudioSyncOffset }
        };

        SendRequest(nameof(SetInputAudioSyncOffset), requestFields);
    }

    public int GetInputAudioSyncOffset(string inputName)
    {
        JObject requestFields = new JObject
        {
            { nameof(inputName), inputName }
        };
        JObject response = SendRequest(nameof(GetInputAudioSyncOffset), requestFields);
        int inputAudioSyncOffset = Convert.ToInt32(response["inputAudioSyncOffset"]);
        return inputAudioSyncOffset;
    }

    public void RemoveSceneItem(string sceneName, int sceneItemId)
    {
        JObject requestFields = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        SendRequest(nameof(RemoveSceneItem), requestFields);
    }

    public void SendStreamCaption(string captionText)
    {
        JObject requestFields = new JObject
        {
            { nameof(captionText), captionText }
        };

        SendRequest(nameof(SendStreamCaption), requestFields);
    }

    public void DuplicateSceneItem(string sceneName, int sceneItemId, string destinationSceneName = "")
    {
        JObject requestFields = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        if (!string.IsNullOrEmpty(destinationSceneName))
            requestFields.Add(nameof(destinationSceneName), destinationSceneName);

        SendRequest(nameof(DuplicateSceneItem), requestFields);
    }

    public Dictionary<string, string> GetSpecialInputs()
    {
        JObject response = SendRequest(nameof(GetSpecialInputs));
        Dictionary<string, string> sources = new Dictionary<string, string>();
        foreach ((string key, JToken jToken) in response)
        {
            string value = (string)jToken ?? "";
            if (key != "requestType")
            {
                sources.Add(key, value);
            }
        }

        return sources;
    }

    public void SetStreamServiceSettings(StreamingService service)
    {
        if(service.Settings == null) return;
        JObject requestFields = new JObject
        {
            { "streamServiceType", service.Type },
            { "streamServiceSettings", JToken.FromObject(service.Settings) }
        };

        SendRequest(nameof(SetStreamServiceSettings), requestFields);
    }
    
    public StreamingService GetStreamServiceSettings()
    {
        JObject response = SendRequest(nameof(GetStreamServiceSettings));
        StreamingService service = JsonConvert.DeserializeObject<StreamingService>(response.ToString()) ??
                                   new StreamingService();
        return service;
    }

    public string GetInputAudioMonitorType(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        JObject response = SendRequest(nameof(GetInputAudioMonitorType), request);
        string monitorType = response["monitorType"]?.ToString() ?? "";
        return monitorType;
    }

    public void SetInputAudioMonitorType(string inputName, string monitorType)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(monitorType), monitorType }
        };

        SendRequest(nameof(SetInputAudioMonitorType), request);
    }

    public void BroadcastCustomEvent(JObject eventData)
    {
        JObject request = new JObject
        {
            { nameof(eventData), eventData }
        };

        SendRequest(nameof(BroadcastCustomEvent), request);
    }

    public void SetMediaInputCursor(string inputName, int mediaCursor)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(mediaCursor), mediaCursor }
        };

        SendRequest(nameof(SetMediaInputCursor), request);
    }

    public void OffsetMediaInputCursor(string inputName, int mediaCursorOffset)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(mediaCursorOffset), mediaCursorOffset }
        };

        SendRequest(nameof(OffsetMediaInputCursor), request);
    }
    
    public int CreateInput(string sceneName, string inputName, string inputKind, JObject inputSettings,
        bool? sceneItemEnabled)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(inputName), inputName },
            { nameof(inputKind), inputKind },
            { nameof(inputSettings), inputSettings }
        };

        if (sceneItemEnabled.HasValue)
        {
            request.Add(nameof(sceneItemEnabled), sceneItemEnabled.Value);
        }

        JObject response = SendRequest(nameof(CreateInput), request);
        int sceneItemId = Convert.ToInt32(response["sceneItemId"]);
        return sceneItemId;
    }
    
    public JObject GetInputDefaultSettings(string inputKind)
    {
        JObject request = new JObject
        {
            { nameof(inputKind), inputKind }
        };

        JObject response = SendRequest(nameof(GetInputDefaultSettings), request);
        JObject defaultInputSettings = (JObject)response["defaultInputSettings"] ?? new JObject();
        return defaultInputSettings;
    }

    public List<SceneItemDetails> GetSceneItemList(string sceneName)
    {
        JObject request = null;
        if (!string.IsNullOrEmpty(sceneName))
        {
            request = new JObject
            {
                { nameof(sceneName), sceneName }
            };
        }

        if (request == null) return new List<SceneItemDetails>();
        
        JObject response = SendRequest(nameof(GetSceneItemList), request);
        return response["sceneItems"]?.Select(m => new SceneItemDetails((JObject)m)).ToList() ?? new List<SceneItemDetails>();
    }

    public int CreateSceneItem(string sceneName, string sourceName, bool sceneItemEnabled = true)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sourceName), sourceName },
            { nameof(sceneItemEnabled), sceneItemEnabled }
        };

        JObject response = SendRequest(nameof(CreateSceneItem), request);
        int sceneItemId = Convert.ToInt32(response["sceneItemId"]);
        return sceneItemId;
    }

    public void CreateScene(string sceneName)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        SendRequest(nameof(CreateScene), request);
    }

    public SourceTracks GetInputAudioTracks(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        JObject response = SendRequest(nameof(GetInputAudioTracks), request);
        return new SourceTracks(response);
    }

    public void SetInputAudioTracks(string inputName, JObject inputAudioTracks)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputAudioTracks), inputAudioTracks }
        };

        SendRequest(nameof(SetInputAudioTracks), request);
    }

    public void SetInputAudioTracks(string inputName, SourceTracks inputAudioTracks)
    {
        SetInputAudioTracks(inputName, JObject.FromObject(inputAudioTracks));
    }

    public SourceActiveInfo GetSourceActive(string sourceName)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName }
        };

        JObject response = SendRequest(nameof(GetSourceActive), request);
        return new SourceActiveInfo(response);
    }

    public VirtualCamStatus GetVirtualCamStatus()
    {
        JObject response = SendRequest(nameof(GetVirtualCamStatus));
        VirtualCamStatus outputStatus = new VirtualCamStatus(response);
        return outputStatus;
    }

    public void StartVirtualCam()
    {
        SendRequest(nameof(StartVirtualCam));
    }

    public void StopVirtualCam()
    {
        SendRequest(nameof(StopVirtualCam));
    }

    public VirtualCamStatus ToggleVirtualCam()
    {
        JObject response = SendRequest(nameof(ToggleVirtualCam));
        VirtualCamStatus outputStatus = new VirtualCamStatus(response);
        return outputStatus;
    }

    public JObject GetPersistentData(string realm, string slotName)
    {
        JObject request = new JObject
        {
            { nameof(realm), realm },
            { nameof(slotName), slotName }
        };

        return SendRequest(nameof(GetPersistentData), request);
    }

    public void SetPersistentData(string realm, string slotName, JObject slotValue)
    {
        JObject request = new JObject
        {
            { nameof(realm), realm },
            { nameof(slotName), slotName },
            { nameof(slotValue), slotValue }
        };

        SendRequest(nameof(SetPersistentData), request);
    }

    public void CreateSceneCollection(string sceneCollectionName)
    {
        JObject request = new JObject
        {
            { nameof(sceneCollectionName), sceneCollectionName }
        };

        SendRequest(nameof(CreateSceneCollection), request);
    }

    public void CreateProfile(string profileName)
    {
        JObject request = new JObject
        {
            { nameof(profileName), profileName }
        };

        SendRequest(nameof(CreateProfile), request);
    }

    public void RemoveProfile(string profileName)
    {
        JObject request = new JObject
        {
            { nameof(profileName), profileName }
        };

        SendRequest(nameof(RemoveProfile), request);
    }

    public JObject GetProfileParameter(string parameterCategory, string parameterName)
    {
        JObject request = new JObject
        {
            { nameof(parameterCategory), parameterCategory },
            { nameof(parameterName), parameterName }
        };

        return SendRequest(nameof(GetProfileParameter), request);
    }

    public void SetProfileParameter(string parameterCategory, string parameterName, string parameterValue)
    {
        JObject request = new JObject
        {
            { nameof(parameterCategory), parameterCategory },
            { nameof(parameterName), parameterName },
            { nameof(parameterValue), parameterValue }
        };

        SendRequest(nameof(SetProfileParameter), request);
    }

    public void SetVideoSettings(ObsVideoSettings obsVideoSettings)
    {
        SendRequest(nameof(SetVideoSettings), JObject.FromObject(obsVideoSettings));
    }

    public JObject GetSourceFilterDefaultSettings(string filterKind)
    {
        JObject request = new JObject
        {
            { nameof(filterKind), filterKind }
        };

        return SendRequest(nameof(GetSourceFilterDefaultSettings), request);
    }

    public void SetSourceFilterName(string sourceName, string filterName, string newFilterName)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(newFilterName), newFilterName }
        };

        SendRequest(nameof(SetSourceFilterName), request);
    }

    public void SetSourceFilterIndex(string sourceName, string filterName, int filterIndex)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(filterIndex), filterIndex }
        };

        SendRequest(nameof(SetSourceFilterIndex), request);
    }

    public ObsVersion GetVersion()
    {
        JObject response = SendRequest(nameof(GetVersion));
        return new ObsVersion(response);
    }

    public JObject CallVendorRequest(string vendorName, string requestType, JObject requestData = null)
    {
        JObject request = new JObject
        {
            { nameof(vendorName), vendorName },
            { nameof(requestType), requestType },
            { nameof(requestData), requestData }
        };

        return SendRequest(nameof(CallVendorRequest), request);
    }

    public List<string> GetHotkeyList()
    {
        JObject response = SendRequest(nameof(GetHotkeyList));
        string hotkeys = response["hotkeys"]?.ToString() ?? "";
        List<string> hotkeysList = JsonConvert.DeserializeObject<List<string>>(hotkeys) ?? new List<string>();
        return hotkeysList;
    }

    public void Sleep(int sleepMillis, int sleepFrames)
    {
        JObject request = new JObject
        {
            { nameof(sleepMillis), sleepMillis },
            { nameof(sleepFrames), sleepFrames }
        };

        SendRequest(nameof(Sleep), request);
    }

    public List<InputBasicInfo> GetInputList(string inputKind = "")
    {
        JObject request = new JObject
        {
            { nameof(inputKind), inputKind }
        };

        JObject response = SendRequest(nameof(GetInputList), request);

        JToken inputs = response["inputs"];
        if (inputs == null) return new List<InputBasicInfo>();
        
        List<InputBasicInfo> returnList = new List<InputBasicInfo>();
        foreach (JToken input in inputs)
        {
            returnList.Add(new InputBasicInfo((JObject)input));
        }

        return returnList;
    }

    public List<string> GetInputKindList(bool unversioned = false)
    {
        JObject request = new JObject
        {
            { nameof(unversioned), unversioned }
        };

        JObject response = unversioned is false
            ? SendRequest(nameof(GetInputKindList))
            : SendRequest(nameof(GetInputKindList), request);

        string inputKinds = response["inputKinds"]?.ToString() ?? "";
        List<string> inputKindList = JsonConvert.DeserializeObject<List<string>>(inputKinds) ?? new List<string>();
        return inputKindList;
    }

    public void RemoveInput(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(RemoveInput), request);
    }

    public void SetInputName(string inputName, string newInputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(newInputName), newInputName }
        };

        SendRequest(nameof(SetInputName), request);
    }

    public InputSettings GetInputSettings(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        JObject response = SendRequest(nameof(GetInputSettings), request);
        response.Merge(request);
        return new InputSettings(response);
    }

    public void SetInputSettings(InputSettings inputSettings, bool overlay = true)
    {
        if (string.IsNullOrEmpty(inputSettings.InputName) || inputSettings.Settings == null) return;
        SetInputSettings(inputSettings.InputName, inputSettings.Settings, overlay);
    }

    public void SetInputSettings(string inputName, JObject inputSettings, bool overlay = true)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputSettings), inputSettings },
            { nameof(overlay), overlay }
        };

        SendRequest(nameof(SetInputSettings), request);
    }

    public double GetInputAudioBalance(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        JObject response = SendRequest(nameof(GetInputAudioBalance), request);
        double inputAudioBalance = Convert.ToDouble(response["inputAudioBalance"]);
        return inputAudioBalance;
    }

    public void SetInputAudioBalance(string inputName, double inputAudioBalance)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputAudioBalance), inputAudioBalance }
        };

        SendRequest(nameof(SetInputAudioBalance), request);
    }

    public List<JObject> GetInputPropertiesListPropertyItems(string inputName, string propertyName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(propertyName), propertyName }
        };

        JObject response = SendRequest(nameof(GetInputPropertiesListPropertyItems), request);
        List<JObject> propertyItems = response["propertyItems"]?.Value<List<JObject>>() ?? new List<JObject>();
        return propertyItems;
    }

    public void PressInputPropertiesButton(string inputName, string propertyName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(propertyName), propertyName }
        };

        SendRequest(nameof(PressInputPropertiesButton), request);
    }

    public MediaInputStatus GetMediaInputStatus(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        return new MediaInputStatus(SendRequest(nameof(GetMediaInputStatus), request));
    }

    public void TriggerMediaInputAction(string inputName, string mediaAction)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(mediaAction), mediaAction }
        };

        SendRequest(nameof(TriggerMediaInputAction), request);
    }

    public string GetLastReplayBufferReplay()
    {
        JObject response = SendRequest(nameof(GetLastReplayBufferReplay));
        string savedReplayPath = response["savedReplayPath"]?.ToString() ?? "";
        return savedReplayPath;
    }

    public void ToggleRecordPause()
    {
        SendRequest(nameof(ToggleRecordPause));
    }

    public List<JObject> GetGroupSceneItemList(string sceneName)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        JObject response = SendRequest(nameof(GetGroupSceneItemList), request);
        string sceneItems = response["sceneItems"]?.ToString() ?? "";
        List<JObject> sceneItemObjects = JsonConvert.DeserializeObject<List<JObject>>(sceneItems) ?? new List<JObject>();
        return sceneItemObjects;
    }

    public int GetSceneItemId(string sceneName, string sourceName, int searchOffset)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sourceName), sourceName },
            { nameof(searchOffset), searchOffset }
        };

        JObject response = SendRequest(nameof(GetSceneItemId), request);
        int sceneItemId = Convert.ToInt32(response["sceneItemId"]);
        return sceneItemId;
    }

    public SceneItemTransformInfo GetSceneItemTransform(string sceneName, int sceneItemId)
    {
        JObject response = GetSceneItemTransformRaw(sceneName, sceneItemId);
        string sceneItemTransform = response["sceneItemTransform"]?.ToString() ?? "";
        SceneItemTransformInfo info = JsonConvert.DeserializeObject<SceneItemTransformInfo>(sceneItemTransform) ?? new SceneItemTransformInfo();
        return info;
    }

    private JObject GetSceneItemTransformRaw(string sceneName, int sceneItemId)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        return SendRequest(nameof(GetSceneItemTransform), request);
    }

    public bool GetSceneItemEnabled(string sceneName, int sceneItemId)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        JObject response = SendRequest(nameof(GetSceneItemEnabled), request);
        bool sceneItemEnabled = Convert.ToBoolean(response["sceneItemEnabled"]);
        return sceneItemEnabled;
    }

    public void SetSceneItemEnabled(string sceneName, int sceneItemId, bool sceneItemEnabled)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemEnabled), sceneItemEnabled }
        };

        SendRequest(nameof(SetSceneItemEnabled), request);
    }

    public bool GetSceneItemLocked(string sceneName, int sceneItemId)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        JObject response = SendRequest(nameof(GetSceneItemLocked), request);
        bool sceneItemLocked = Convert.ToBoolean(response["sceneItemLocked"]);
        return sceneItemLocked;
    }

    public void SetSceneItemLocked(string sceneName, int sceneItemId, bool sceneItemLocked)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemLocked), sceneItemLocked }
        };

        SendRequest(nameof(SetSceneItemLocked), request);
    }

    public int GetSceneItemIndex(string sceneName, int sceneItemId)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        JObject response = SendRequest(nameof(GetSceneItemIndex), request);
        int sceneItemIndex = Convert.ToInt32(response["sceneItemIndex"]);
        return sceneItemIndex;
    }

    public void SetSceneItemIndex(string sceneName, int sceneItemId, int sceneItemIndex)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemIndex), sceneItemIndex }
        };

        SendRequest(nameof(SetSceneItemIndex), request);
    }

    public string GetSceneItemBlendMode(string sceneName, int sceneItemId)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        JObject response = SendRequest(nameof(GetSceneItemBlendMode), request);
        string sceneItemBlendMode = response["sceneItemBlendMode"]?.ToString() ?? "";
        return sceneItemBlendMode;
    }

    public void SetSceneItemBlendMode(string sceneName, int sceneItemId, string sceneItemBlendMode)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemBlendMode), sceneItemBlendMode }
        };

        SendRequest(nameof(SetSceneItemBlendMode), request);
    }

    public List<string> GetGroupList()
    {
        JObject response = SendRequest(nameof(GetGroupList));
        string groupName = response["groups"]?.ToString() ?? "";
        List<string> groups = JsonConvert.DeserializeObject<List<string>>(groupName) ?? new List<string>();
        return groups;
    }

    public void RemoveScene(string sceneName)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        SendRequest(nameof(RemoveScene), request);
    }

    public void SetSceneName(string sceneName, string newSceneName)
    {
        JObject request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(newSceneName), newSceneName }
        };

        SendRequest(nameof(SetSceneName), request);
    }

    public string GetSourceScreenshot(string sourceName, string imageFormat, int imageWidth = -1, int imageHeight = -1,
        int imageCompressionQuality = -1)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(imageFormat), imageFormat }
        };

        if (imageWidth > -1)
        {
            request.Add(nameof(imageWidth), imageWidth);
        }

        if (imageHeight > -1)
        {
            request.Add(nameof(imageHeight), imageHeight);
        }

        if (imageCompressionQuality > -1)
        {
            request.Add(nameof(imageCompressionQuality), imageCompressionQuality);
        }

        JObject response = SendRequest(nameof(GetSourceScreenshot), request);
        string imageData = response["imageData"]?.ToString() ?? "";
        return imageData;
    }

    public List<string> GetTransitionKindList()
    {
        JObject response = SendRequest(nameof(GetTransitionKindList));
        string transitionKinds = response["transitionKinds"]?.ToString() ?? "";
        List<string> transitionKindList = JsonConvert.DeserializeObject<List<string>>(transitionKinds) ?? new List<string>();
        return transitionKindList;
    }

    public double GetCurrentSceneTransitionCursor()
    {
        JObject response = SendRequest(nameof(GetCurrentSceneTransitionCursor));
        double transitionCursor = Convert.ToDouble(response["transitionCursor"]);
        return transitionCursor;
    }

    public void OpenInputPropertiesDialog(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(OpenInputPropertiesDialog), request);
    }

    public void OpenInputFiltersDialog(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(OpenInputFiltersDialog), request);
    }

    public void OpenInputInteractDialog(string inputName)
    {
        JObject request = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(OpenInputInteractDialog), request);
    }

    public List<Monitor> GetMonitorList()
    {
        JObject response = SendRequest(nameof(GetMonitorList));
        List<Monitor> monitors = new List<Monitor>();
        JToken monitorObj = response["monitors"];
        if (monitorObj == null) return new List<Monitor>();
        
        foreach (JToken monitor in monitorObj)
        {
            monitors.Add(new Monitor((JObject)monitor));
        }

        return monitors;
    }

    public void OpenSourceProjector(string sourceName, string projectorGeometry, int monitorIndex = -1)
    {
        JObject request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(projectorGeometry), projectorGeometry },
            { nameof(monitorIndex), monitorIndex },
        };

        SendRequest(nameof(OpenSourceProjector), request);
    }

    public void OpenVideoMixProjector(string videoMixType, string projectorGeometry, int monitorIndex = -1)
    {
        JObject request = new JObject
        {
            { nameof(videoMixType), videoMixType },
            { nameof(projectorGeometry), projectorGeometry },
            { nameof(monitorIndex), monitorIndex },
        };

        SendRequest(nameof(OpenVideoMixProjector), request);
    }

    private void ProcessEventType(string eventType, JObject body)
    {
        JObject bodyObj = (JObject)body["eventData"];
        if (bodyObj == null) return;

        string profileName;
        string sceneName;
        string sceneCollectionName;
        string sceneItemIdx;
        string sourceName;
        string transitionName;
        string inputName;
        string filterName;

        int sceneItemId;

        bool sceneItemEnabled;
        bool isGroup;
        
        switch (eventType)
        {
            case nameof(OnCurrentProgramSceneChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                OnCurrentProgramSceneChanged?.Invoke(this, new ProgramSceneChangedEventArgs(sceneName));
                break;
            case nameof(OnSceneListChanged):
                string scenes = bodyObj["scenes"]?.ToString() ?? "";
                List<JObject> sceneList = JsonConvert.DeserializeObject<List<JObject>>(scenes) ?? new List<JObject>();
                OnSceneListChanged?.Invoke(this,
                    new SceneListChangedEventArgs(sceneList));
                break;
            case nameof(OnSceneItemListReindexed):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                string sceneItems = bodyObj["sceneItems"]?.ToString() ?? "";
                List<JObject> sceneItemsList =
                    JsonConvert.DeserializeObject<List<JObject>>(sceneItems) ?? new List<JObject>();
                OnSceneItemListReindexed?.Invoke(this,
                    new SceneItemListReindexedEventArgs(sceneName, sceneItemsList));
                break;
            case nameof(OnSceneItemCreated):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                sceneItemId = Convert.ToInt32(bodyObj["sceneItemId"]);
                int sceneItemIndex = Convert.ToInt32(bodyObj["sceneItemIndex"]);
                OnSceneItemCreated?.Invoke(this,
                    new SceneItemCreatedEventArgs(sceneName, sourceName, sceneItemId, sceneItemIndex));
                break;
            case nameof(OnSceneItemRemoved):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                sceneItemId = Convert.ToInt32(bodyObj["sceneItemId"]);

                OnSceneItemRemoved?.Invoke(this,
                    new SceneItemRemovedEventArgs(sceneName, sourceName, sceneItemId));
                break;
            case nameof(OnSceneItemEnableStateChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItemId = Convert.ToInt32(bodyObj["sceneItemId"]);
                sceneItemEnabled = Convert.ToBoolean(bodyObj["sceneItemEnabled"]);

                OnSceneItemEnableStateChanged?.Invoke(this,
                    new SceneItemEnableStateChangedEventArgs(sceneName, sceneItemId, sceneItemEnabled));
                break;
            case nameof(OnSceneItemLockStateChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItemId = Convert.ToInt32(bodyObj["sceneItemId"]);
                sceneItemEnabled = Convert.ToBoolean(bodyObj["sceneItemEnabled"]);

                OnSceneItemLockStateChanged?.Invoke(this,
                    new SceneItemLockStateChangedEventArgs(sceneName, sceneItemId, sceneItemEnabled));
                break;
            case nameof(OnCurrentSceneCollectionChanged):
                sceneCollectionName = bodyObj["sceneCollectionName"]?.ToString() ?? "";
                OnCurrentSceneCollectionChanged?.Invoke(this,
                    new CurrentSceneCollectionChangedEventArgs(sceneCollectionName));
                break;
            case nameof(OnSceneCollectionListChanged):
                string sceneCollections = bodyObj["sceneCollections"]?.ToString() ?? "";
                List<string> sceneCollectionsList = JsonConvert.DeserializeObject<List<string>>(sceneCollections) ?? new List<string>();

                OnSceneCollectionListChanged?.Invoke(this,
                    new SceneCollectionListChangedEventArgs(sceneCollectionsList));
                break;
            case nameof(OnCurrentSceneTransitionChanged):
                transitionName = bodyObj["transitionName"]?.ToString() ?? "";
                OnCurrentSceneTransitionChanged?.Invoke(this,
                    new CurrentSceneTransitionChangedEventArgs(transitionName));
                break;
            case nameof(OnCurrentSceneTransitionDurationChanged):
                int transitionDuration = Convert.ToInt32(bodyObj["transitionDuration"]);
                OnCurrentSceneTransitionDurationChanged?.Invoke(this,
                    new CurrentSceneTransitionDurationChangedEventArgs(transitionDuration));
                break;
            case nameof(OnSceneTransitionStarted):
                transitionName = bodyObj["transitionName"]?.ToString() ?? "";
                OnSceneTransitionStarted?.Invoke(this,
                    new SceneTransitionStartedEventArgs(transitionName));
                break;
            case nameof(OnSceneTransitionEnded):
                transitionName = bodyObj["transitionName"]?.ToString() ?? "";
                OnSceneTransitionEnded?.Invoke(this, new SceneTransitionEndedEventArgs(transitionName));
                break;
            case nameof(OnSceneTransitionVideoEnded):
                transitionName = bodyObj["transitionName"]?.ToString() ?? "";
                OnSceneTransitionVideoEnded?.Invoke(this,
                    new SceneTransitionVideoEndedEventArgs(transitionName));
                break;
            case nameof(OnCurrentProfileChanged):
                profileName = bodyObj["profileName"]?.ToString() ?? "";
                OnCurrentProfileChanged?.Invoke(this, new CurrentProfileChangedEventArgs(profileName));
                break;
            case nameof(OnProfileListChanged):
                string profiles = bodyObj["profiles"]?.ToString() ?? "";
                List<string> profileList = JsonConvert.DeserializeObject<List<string>>(profiles) ?? new List<string>();
                OnProfileListChanged?.Invoke(this, new ProfileListChangedEventArgs(profileList));
                break;
            case nameof(OnStreamStateChanged):
                OnStreamStateChanged?.Invoke(this, new StreamStateChangedEventArgs(new OutputStateChanged(body)));
                break;
            case nameof(OnRecordStateChanged):
                OnRecordStateChanged?.Invoke(this, new RecordStateChangedEventArgs(new RecordStateChanged(body)));
                break;
            case nameof(OnCurrentPreviewSceneChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                
                OnCurrentPreviewSceneChanged?.Invoke(this, new CurrentPreviewSceneChangedEventArgs(sceneName));
                break;
            case nameof(OnStudioModeStateChanged):
                bool studioModeEnabled = Convert.ToBoolean(bodyObj["studioModeEnabled"]);
                OnStudioModeStateChanged?.Invoke(this,
                    new StudioModeStateChangedEventArgs(studioModeEnabled));
                break;
            case nameof(OnReplayBufferStateChanged):
                OnReplayBufferStateChanged?.Invoke(this,
                    new ReplayBufferStateChangedEventArgs(new OutputStateChanged(body)));
                break;
            case nameof(OnExitStarted):
                OnExitStarted?.Invoke(this, EventArgs.Empty);
                break;
            case nameof(OnSceneItemSelected):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItemIdx = bodyObj["sceneItemId"]?.ToString() ?? "";
                OnSceneItemSelected?.Invoke(this, new SceneItemSelectedEventArgs(sceneName, sceneItemIdx));
                break;
            case nameof(OnSceneItemTransformChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItemIdx = bodyObj["sceneItemId"]?.ToString() ?? "";
                JObject sceneItemTransform = (JObject)bodyObj["sceneItemTransform"] ?? new JObject();
                OnSceneItemTransformChanged?.Invoke(this,
                    new SceneItemTransformEventArgs(sceneName, sceneItemIdx,
                        new SceneItemTransformInfo(sceneItemTransform)));
                break;
            case nameof(OnInputAudioSyncOffsetChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                int inputAudioSyncOffset = Convert.ToInt32(bodyObj["inputAudioSyncOffset"]);
                OnInputAudioSyncOffsetChanged?.Invoke(this,
                    new InputAudioSyncOffsetChangedEventArgs(inputName, inputAudioSyncOffset));
                break;
            case nameof(OnInputMuteStateChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                bool inputMuted = Convert.ToBoolean(bodyObj["inputMuted"]);
                OnInputMuteStateChanged?.Invoke(this,
                    new InputMuteStateChangedEventArgs(inputName, inputMuted));
                break;
            case nameof(OnInputVolumeChanged):
                OnInputVolumeChanged?.Invoke(this, new InputVolumeChangedEventArgs(new InputVolume(body)));
                break;
            case nameof(OnSourceFilterCreated):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                filterName = bodyObj["filterName"]?.ToString() ?? "";
                string filterKind = bodyObj["filterKind"]?.ToString() ?? "";
                int filterIndex = Convert.ToInt32(bodyObj["filterIndex"]);
                JObject filterSettings = (JObject)bodyObj["filterSettings"] ?? new JObject();
                JObject defaultFilterSettings = (JObject)bodyObj["defaultFilterSettings"] ?? new JObject();
                OnSourceFilterCreated?.Invoke(this,
                    new SourceFilterCreatedEventArgs(sourceName, filterName,
                        filterKind, filterIndex, filterSettings, defaultFilterSettings));
                break;
            case nameof(OnSourceFilterRemoved):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                filterName = bodyObj["filterName"]?.ToString() ?? "";
                
                OnSourceFilterRemoved?.Invoke(this,
                    new SourceFilterRemovedEventArgs(sourceName, filterName));
                break;
            case nameof(OnSourceFilterListReindexed):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                string filterObj = bodyObj["filters"]?.ToString() ?? "";
                
                List<FilterReorderItem> filters = new List<FilterReorderItem>();
                JsonConvert.PopulateObject(filterObj, filters);

                OnSourceFilterListReindexed?.Invoke(this,
                    new SourceFilterListReindexedEventArgs(sourceName, filters));
                
                break;
            case nameof(OnSourceFilterEnableStateChanged):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                filterName = bodyObj["filterName"]?.ToString() ?? "";
                bool filterEnabled = Convert.ToBoolean(bodyObj["filterEnabled"]);
                OnSourceFilterEnableStateChanged?.Invoke(this,
                    new SourceFilterEnableStateChangedEventArgs(sourceName, filterName, filterEnabled));
                break;
            case nameof(OnVendorEvent):
                string vendorName = bodyObj["vendorName"]?.ToString() ?? "";
                string @event = bodyObj["event"]?.ToString() ?? "";
                OnVendorEvent?.Invoke(this,
                    new VendorEventArgs(vendorName, @event, body));
                break;
            case nameof(OnMediaInputPlaybackEnded):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                OnMediaInputPlaybackEnded?.Invoke(this, new MediaInputPlaybackEndedEventArgs(inputName));
                break;
            case nameof(OnMediaInputPlaybackStarted):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                OnMediaInputPlaybackStarted?.Invoke(this,
                    new MediaInputPlaybackStartedEventArgs(sourceName));
                break;
            case nameof(OnMediaInputActionTriggered):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                string mediaAction = bodyObj["mediaAction"]?.ToString() ?? "";
                OnMediaInputActionTriggered?.Invoke(this,
                    new MediaInputActionTriggeredEventArgs(inputName, mediaAction));
                break;
            case nameof(OnVirtualcamStateChanged):
                OnVirtualcamStateChanged?.Invoke(this, new VirtualcamStateChangedEventArgs(new OutputStateChanged(body)));
                break;
            case nameof(OnCurrentSceneCollectionChanging):
                sceneCollectionName = bodyObj["sceneCollectionName"]?.ToString() ?? "";
                OnCurrentSceneCollectionChanging?.Invoke(this,
                    new CurrentSceneCollectionChangingEventArgs(sceneCollectionName));
                break;
            case nameof(OnCurrentProfileChanging):
                profileName = bodyObj["profileName"]?.ToString() ?? "";
                OnCurrentProfileChanging?.Invoke(this, new CurrentProfileChangingEventArgs(profileName));
                break;
            case nameof(OnSourceFilterNameChanged):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                string oldFilterName = bodyObj["oldFilterName"]?.ToString() ?? "";
                filterName = bodyObj["filterName"]?.ToString() ?? "";
                OnSourceFilterNameChanged?.Invoke(this,
                    new SourceFilterNameChangedEventArgs(sourceName, oldFilterName, filterName));
                break;
            case nameof(OnInputCreated):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                string inputKind = bodyObj["inputKind"]?.ToString() ?? "";
                string unversionedInputKind = bodyObj["unversionedInputKind"]?.ToString() ?? "";
                JObject inputSettings = (JObject)bodyObj["inputSettings"] ?? new JObject();
                JObject defaultInputSettings = (JObject)bodyObj["defaultInputSettings"] ?? new JObject();
                OnInputCreated?.Invoke(this,
                    new InputCreatedEventArgs(inputName, inputKind, unversionedInputKind, 
                        inputSettings, defaultInputSettings));
                break;
            case nameof(OnInputRemoved):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                OnInputRemoved?.Invoke(this, new InputRemovedEventArgs(inputName));
                break;
            case nameof(OnInputNameChanged):
                string oldInputName = bodyObj["oldInputName"]?.ToString() ?? "";
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                OnInputNameChanged?.Invoke(this,
                    new InputNameChangedEventArgs(oldInputName, inputName));
                break;
            case nameof(OnInputActiveStateChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                bool videoActive = Convert.ToBoolean(bodyObj["videoActive"]);
                OnInputActiveStateChanged?.Invoke(this,
                    new InputActiveStateChangedEventArgs(inputName, videoActive));
                break;
            case nameof(OnInputShowStateChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                bool videoShowing = Convert.ToBoolean(bodyObj["videoShowing"]);

                OnInputShowStateChanged?.Invoke(this,
                    new InputShowStateChangedEventArgs(inputName, videoShowing));
                break;
            case nameof(OnInputAudioBalanceChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                double inputAudioBalance = Convert.ToDouble(body["inputAudioBalance"]);
                OnInputAudioBalanceChanged?.Invoke(this,
                    new InputAudioBalanceChangedEventArgs(inputName, inputAudioBalance));
                break;
            case nameof(OnInputAudioTracksChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                JObject inputAudioTrack = (JObject)bodyObj["inputAudioTracks"] ?? new JObject();
                OnInputAudioTracksChanged?.Invoke(this,
                    new InputAudioTracksChangedEventArgs(inputName, inputAudioTrack));
                break;
            case nameof(OnInputAudioMonitorTypeChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                string monitorType = bodyObj["monitorType"]?.ToString() ?? "";
                
                OnInputAudioMonitorTypeChanged?.Invoke(this,
                    new InputAudioMonitorTypeChangedEventArgs(inputName, monitorType));
                break;
            case nameof(OnInputVolumeMeters):
                string inputs = bodyObj["inputs"]?.ToString() ?? "";
                List<JObject> inputList = JsonConvert.DeserializeObject<List<JObject>>(inputs) ?? new List<JObject>();
                OnInputVolumeMeters?.Invoke(this,
                    new InputVolumeMetersEventArgs(inputList));
                break;
            case nameof(OnReplayBufferSaved):
                string savedReplayPath = bodyObj["savedReplayPath"]?.ToString() ?? "";
                OnReplayBufferSaved?.Invoke(this, new ReplayBufferSavedEventArgs(savedReplayPath));
                break;
            case nameof(OnSceneCreated):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                isGroup = Convert.ToBoolean(bodyObj["isGroup"]);
                
                OnSceneCreated?.Invoke(this, new SceneCreatedEventArgs(sceneName, isGroup));
                break;
            case nameof(OnSceneRemoved):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                isGroup = Convert.ToBoolean(bodyObj["isGroup"]);
                OnSceneRemoved?.Invoke(this, new SceneRemovedEventArgs(sceneName, isGroup));
                break;
            case nameof(OnSceneNameChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                string oldSceneName = bodyObj["oldSceneName"]?.ToString() ?? "";
                OnSceneNameChanged?.Invoke(this,
                    new SceneNameChangedEventArgs(oldSceneName, sceneName));
                break;
            default:
                string message = $"Unsupported Event: {eventType}\n{body}";
                _logger.LogWarning(message);
                break;
        }
    }
}