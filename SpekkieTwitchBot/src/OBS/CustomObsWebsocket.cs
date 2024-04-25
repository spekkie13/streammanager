using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS;
using SpekkieClassLibrary.OBS.Communication;
using SpekkieClassLibrary.OBS.Enum;
using SpekkieClassLibrary.OBS.Events;
using SpekkieClassLibrary.OBS.Interface;
using SpekkieClassLibrary.OBS.Types;
using Websocket.Client;
using Monitor = SpekkieClassLibrary.OBS.Types.Monitor;

#nullable disable
namespace SpekkieTwitchBot.OBS;

public class CustomObsWebsocket : IObsWebsocket
{
    private const string WebsocketUrlPrefix = "ws://";
    private const int SupportedRpcVersion = 1;
    private TimeSpan _wsTimeout = TimeSpan.FromSeconds(10);
    private string _connectionPassword;
    private WebsocketClient _wsConnection;

    private delegate void RequestCallback(CustomObsWebsocket sender, JObject body);

    private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> _responseHandlers;
    private static readonly Random Random = new Random();

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

    public CustomObsWebsocket(WebsocketClient wsConnection)
    {
        _wsConnection = wsConnection;
        _responseHandlers = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();
    }

    List<Monitor> IObsWebsocket.GetMonitorList()
    {
        throw new NotImplementedException();
    }

    [Obsolete("Please use ConnectAsync, this function will be removed in the next version")]
    public void Connect(string url, string password)
    {
        ConnectAsync(url, password);
    }

    public void ConnectAsync(string url, string password)
    {
        Console.WriteLine($"url: {url}");
        if (!url.ToLower().StartsWith(WebsocketUrlPrefix))
        {
            throw new ArgumentException($"Invalid url, must start with '{WebsocketUrlPrefix}'");
        }

        if (_wsConnection != null && _wsConnection.IsRunning)
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
        _connectionPassword = "";
        try
        {
            _wsConnection.Stop(WebSocketCloseStatus.NormalClosure, "User requested disconnect");
            ((IDisposable)_wsConnection).Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        _wsConnection = new WebsocketClient(new Uri(""));

        var unusedHandlers = _responseHandlers.ToArray();
        _responseHandlers.Clear();
        foreach (var cb in unusedHandlers)
        {
            var tcs = cb.Value;
            tcs.TrySetCanceled();
        }
    }

    private void OnWebsocketDisconnect(object sender, DisconnectionInfo d)
    {
        Disconnected.Invoke(sender,
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
                Task.Run(() => Connected?.Invoke(this, EventArgs.Empty));
                break;
            case MessageTypes.RequestResponse:
            case MessageTypes.RequestBatchResponse:
                if (body.TryGetValue("requestId", out var value))
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

    public JObject SendRequest(string requestType, JObject additionalFields = null)
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

        var tcs = new TaskCompletionSource<JObject>();
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
        if (!waitForReply)
        {
            return null;
        }

        tcs.Task.Wait(_wsTimeout.Milliseconds);

        if (tcs.Task.IsCanceled)
            throw new ErrorResponseException("Request canceled", 0);

        var result = tcs.Task.Result;
        JToken requestStatus = result["requestStatus"] ?? new JObject();
        bool reqStatus = Convert.ToBoolean(requestStatus["result"]);
        if (!reqStatus)
        {
            var status = (JObject)result["requestStatus"];
            var code = result["code"]?.ToString() ?? "";
            throw new ErrorResponseException(
                $"ErrorCode: {code}{(status != null && status.TryGetValue("comment", out var s) ? $", Comment: {s}" : "")}",
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

    public event EventHandler<ProgramSceneChangedEventArgs> CurrentProgramSceneChanged;
    public event EventHandler<SceneListChangedEventArgs> SceneListChanged;
    public event EventHandler<SceneItemListReindexedEventArgs> SceneItemListReindexed;
    public event EventHandler<SceneItemCreatedEventArgs> SceneItemCreated;
    public event EventHandler<SceneItemRemovedEventArgs> SceneItemRemoved;
    public event EventHandler<SceneItemEnableStateChangedEventArgs> SceneItemEnableStateChanged;
    public event EventHandler<SceneItemLockStateChangedEventArgs> SceneItemLockStateChanged;
    public event EventHandler<CurrentSceneCollectionChangedEventArgs> CurrentSceneCollectionChanged;
    public event EventHandler<SceneCollectionListChangedEventArgs> SceneCollectionListChanged;
    public event EventHandler<CurrentSceneTransitionChangedEventArgs> CurrentSceneTransitionChanged;
    public event EventHandler<CurrentSceneTransitionDurationChangedEventArgs> CurrentSceneTransitionDurationChanged;
    public event EventHandler<SceneTransitionStartedEventArgs> SceneTransitionStarted;
    public event EventHandler<SceneTransitionEndedEventArgs> SceneTransitionEnded;
    public event EventHandler<SceneTransitionVideoEndedEventArgs> SceneTransitionVideoEnded;
    public event EventHandler<CurrentProfileChangedEventArgs> CurrentProfileChanged;
    public event EventHandler<ProfileListChangedEventArgs> ProfileListChanged;
    public event EventHandler<StreamStateChangedEventArgs> StreamStateChanged;
    public event EventHandler<RecordStateChangedEventArgs> RecordStateChanged;
    public event EventHandler<ReplayBufferStateChangedEventArgs> ReplayBufferStateChanged;
    public event EventHandler<CurrentPreviewSceneChangedEventArgs> CurrentPreviewSceneChanged;
    public event EventHandler<StudioModeStateChangedEventArgs> StudioModeStateChanged;
    public event EventHandler ExitStarted;
    public event EventHandler Connected;
    public event EventHandler<ObsDisconnectionInfo> Disconnected;
    public event EventHandler<SceneItemSelectedEventArgs> SceneItemSelected;
    public event EventHandler<SceneItemTransformEventArgs> SceneItemTransformChanged;
    public event EventHandler<InputAudioSyncOffsetChangedEventArgs> InputAudioSyncOffsetChanged;
    public event EventHandler<SourceFilterCreatedEventArgs> SourceFilterCreated;
    public event EventHandler<SourceFilterRemovedEventArgs> SourceFilterRemoved;
    public event EventHandler<SourceFilterListReindexedEventArgs> SourceFilterListReindexed;
    public event EventHandler<SourceFilterEnableStateChangedEventArgs> SourceFilterEnableStateChanged;
    public event EventHandler<InputMuteStateChangedEventArgs> InputMuteStateChanged;
    public event EventHandler<InputVolumeChangedEventArgs> InputVolumeChanged;
    public event EventHandler<VendorEventArgs> VendorEvent;
    public event EventHandler<MediaInputPlaybackEndedEventArgs> MediaInputPlaybackEnded;
    public event EventHandler<MediaInputPlaybackStartedEventArgs> MediaInputPlaybackStarted;
    public event EventHandler<MediaInputActionTriggeredEventArgs> MediaInputActionTriggered;
    public event EventHandler<VirtualcamStateChangedEventArgs> VirtualcamStateChanged;
    public event EventHandler<CurrentSceneCollectionChangingEventArgs> CurrentSceneCollectionChanging;
    public event EventHandler<CurrentProfileChangingEventArgs> CurrentProfileChanging;
    public event EventHandler<SourceFilterNameChangedEventArgs> SourceFilterNameChanged;
    public event EventHandler<InputCreatedEventArgs> InputCreated;
    public event EventHandler<InputRemovedEventArgs> InputRemoved;
    public event EventHandler<InputNameChangedEventArgs> InputNameChanged;
    public event EventHandler<InputActiveStateChangedEventArgs> InputActiveStateChanged;
    public event EventHandler<InputShowStateChangedEventArgs> InputShowStateChanged;
    public event EventHandler<InputAudioBalanceChangedEventArgs> InputAudioBalanceChanged;
    public event EventHandler<InputAudioTracksChangedEventArgs> InputAudioTracksChanged;
    public event EventHandler<InputAudioMonitorTypeChangedEventArgs> InputAudioMonitorTypeChanged;
    public event EventHandler<InputVolumeMetersEventArgs> InputVolumeMeters;
    public event EventHandler<ReplayBufferSavedEventArgs> ReplayBufferSaved;
    public event EventHandler<SceneCreatedEventArgs> SceneCreated;
    public event EventHandler<SceneRemovedEventArgs> SceneRemoved;
    public event EventHandler<SceneNameChangedEventArgs> SceneNameChanged;

    private void SendIdentify(string password, ObsAuthInfo authInfo)
    {
        var requestFields = new JObject
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
        using var sha256 = SHA256.Create();

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
        if (!_wsConnection.IsStarted)
        {
            return;
        }

        ObsAuthInfo authInfo = new ObsAuthInfo();
        if (payload.TryGetValue("authentication", out var value))
        {
            authInfo = new ObsAuthInfo((JObject)value);
        }

        SendIdentify(_connectionPassword, authInfo);

        _connectionPassword = "";
    }

    private const string REQUEST_FIELD_VOLUME_DB = "inputVolumeDb";
    private const string REQUEST_FIELD_VOLUME_MUL = "inputVolumeMul";
    private const string RESPONSE_FIELD_IMAGE_DATA = "imageData";

    public ObsVideoSettings GetVideoSettings()
    {
        JObject response = SendRequest(nameof(GetVideoSettings));
        ObsVideoSettings settings = JsonConvert.DeserializeObject<ObsVideoSettings>(response.ToString()) ?? new ObsVideoSettings();
        return settings;
    }

    ObsVideoSettings IObsWebsocket.GetVideoSettings()
    {
        return GetVideoSettings();
    }

    public string SaveSourceScreenshot(string sourceName, string imageFormat, string imageFilePath, int imageWidth = -1,
        int imageHeight = -1, int imageCompressionQuality = -1)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(imageFormat), imageFormat },
            { nameof(imageFilePath), imageFilePath }
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

        var response = SendRequest(nameof(SaveSourceScreenshot), request);
        string imageData = response["imageData"]?.ToString() ?? "";
        return imageData;
    }

    public string SaveSourceScreenshot(string sourceName, string imageFormat, string imageFilePath)
    {
        return SaveSourceScreenshot(sourceName, imageFormat, imageFilePath, -1);
    }

    public void TriggerHotkeyByName(string hotkeyName)
    {
        var request = new JObject
        {
            { nameof(hotkeyName), hotkeyName }
        };

        SendRequest(nameof(TriggerHotkeyByName), request);
    }

    public void TriggerHotkeyByKeySequence(OBSHotkey keyId, KeyModifier keyModifier = KeyModifier.None)
    {
        var request = new JObject
        {
            { nameof(keyId), keyId.ToString() },
            {
                "keyModifiers", new JObject
                {
                    { "shift", (keyModifier & KeyModifier.Shift) == KeyModifier.Shift },
                    { "alt", (keyModifier & KeyModifier.Alt) == KeyModifier.Alt },
                    { "control", (keyModifier & KeyModifier.Control) == KeyModifier.Control },
                    { "command", (keyModifier & KeyModifier.Command) == KeyModifier.Command }
                }
            }
        };

        SendRequest(nameof(TriggerHotkeyByKeySequence), request);
    }

    public string GetCurrentProgramScene()
    {
        JObject response = SendRequest(nameof(GetCurrentProgramScene));
        string currentProgramSceneName = response["currentProgramSceneName"]?.ToString() ?? "";
        return currentProgramSceneName;
    }

    public void SetCurrentProgramScene(string sceneName)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        SendRequest(nameof(SetCurrentProgramScene), request);
    }

    ObsStats IObsWebsocket.GetStats()
    {
        return GetStats();
    }

    List<SceneBasicInfo> IObsWebsocket.ListScenes()
    {
        return ListScenes();
    }

    GetSceneListInfo IObsWebsocket.GetSceneList()
    {
        return GetSceneList();
    }

    TransitionOverrideInfo IObsWebsocket.GetSceneSceneTransitionOverride(string sceneName)
    {
        return GetSceneSceneTransitionOverride(sceneName);
    }

    public ObsStats GetStats()
    {
        JObject response = SendRequest(nameof(GetStats));
        ObsStats stats = JsonConvert.DeserializeObject<ObsStats>(response.ToString()) ?? new ObsStats();
        return stats;
    }

    private List<SceneBasicInfo> ListScenes()
    {
        var response = GetSceneList();
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
        var request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        JObject response = SendRequest(nameof(GetSceneSceneTransitionOverride), request);
        TransitionOverrideInfo info = response.ToObject<TransitionOverrideInfo>() ?? new TransitionOverrideInfo();
        return info;
    }

    public void SetSceneSceneTransitionOverride(string sceneName, string transitionName, int transitionDuration = -1)
    {
        var request = new JObject
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
        if (position < 0.0 || position > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        var request = new JObject
        {
            { nameof(position), position },
            { nameof(release), release }
        };

        SendRequest(nameof(SetTBarPosition), request);
    }

    public void SetSourceFilterSettings(string sourceName, string filterName, JObject filterSettings,
        bool overlay = false)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(filterSettings), filterSettings },
            { nameof(overlay), overlay }
        };

        SendRequest(nameof(SetSourceFilterSettings), request);
    }

    void IObsWebsocket.SetSourceFilterSettings(string sourceName, string filterName, FilterSettings filterSettings,
        bool overlay)
    {
        SetSourceFilterSettings(sourceName, filterName, filterSettings, overlay);
    }

    public void SetSourceFilterSettings(string sourceName, string filterName, FilterSettings filterSettings,
        bool overlay = false)
    {
        SetSourceFilterSettings(sourceName, filterName, JObject.FromObject(filterSettings), overlay);
    }


    public void SetSourceFilterEnabled(string sourceName, string filterName, bool filterEnabled)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(filterEnabled), filterEnabled }
        };

        SendRequest(nameof(SetSourceFilterEnabled), request);
    }

    List<FilterSettings> IObsWebsocket.GetSourceFilterList(string sourceName)
    {
        return GetSourceFilterList(sourceName);
    }

    FilterSettings IObsWebsocket.GetSourceFilter(string sourceName, string filterName)
    {
        return GetSourceFilter(sourceName, filterName);
    }

    public List<FilterSettings> GetSourceFilterList(string sourceName)
    {
        var request = new JObject
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
        var request = new JObject
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
        var request = new JObject
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
            Console.WriteLine(e.Message);
        }

        return false;
    }

    public void CreateSourceFilter(string sourceName, string filterName, string filterKind, JObject filterSettings)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(filterKind), filterKind },
            { nameof(filterSettings), filterSettings }
        };

        SendRequest(nameof(CreateSourceFilter), request);
    }

    void IObsWebsocket.CreateSourceFilter(string sourceName, string filterName, string filterKind,
        FilterSettings filterSettings)
    {
        CreateSourceFilter(sourceName, filterName, filterKind, filterSettings);
    }

    public void CreateSourceFilter(string sourceName, string filterName, string filterKind,
        FilterSettings filterSettings)
    {
        CreateSourceFilter(sourceName, filterName, filterKind, JObject.FromObject(filterSettings));
    }

    public bool ToggleStream()
    {
        var response = SendRequest(nameof(ToggleStream));
        bool outputActive = Convert.ToBoolean(response["outputActive"]);
        return outputActive;
    }

    public void ToggleRecord()
    {
        SendRequest(nameof(ToggleRecord));
    }

    OutputStatus IObsWebsocket.GetStreamStatus()
    {
        return GetStreamStatus();
    }

    TransitionSettings IObsWebsocket.GetCurrentSceneTransition()
    {
        return GetCurrentSceneTransition();
    }

    public OutputStatus GetStreamStatus()
    {
        var response = SendRequest(nameof(GetStreamStatus));
        var outputStatus = new OutputStatus(response);
        return outputStatus;
    }

    public TransitionSettings GetCurrentSceneTransition()
    {
        var response = SendRequest(nameof(GetCurrentSceneTransition));
        return new TransitionSettings(response);
    }

    public void SetCurrentSceneTransition(string transitionName)
    {
        var request = new JObject
        {
            { nameof(transitionName), transitionName }
        };

        SendRequest(nameof(SetCurrentSceneTransition), request);
    }

    public void SetCurrentSceneTransitionDuration(int transitionDuration)
    {
        var request = new JObject
        {
            { nameof(transitionDuration), transitionDuration }
        };

        SendRequest(nameof(SetCurrentSceneTransitionDuration), request);
    }

    public void SetCurrentSceneTransitionSettings(JObject transitionSettings, bool overlay)
    {
        var requestFields = new JObject
        {
            { nameof(transitionSettings), JToken.FromObject(transitionSettings) },
            { nameof(overlay), overlay }
        };

        SendRequest(nameof(SetCurrentSceneTransitionSettings), requestFields);
    }

    public void SetInputVolume(string inputName, float inputVolume, bool inputVolumeDb = false)
    {
        var requestFields = new JObject
        {
            { nameof(inputName), inputName }
        };

        requestFields.Add(inputVolumeDb ? REQUEST_FIELD_VOLUME_DB : REQUEST_FIELD_VOLUME_MUL, inputVolume);

        SendRequest(nameof(SetInputVolume), requestFields);
    }

    VolumeInfo IObsWebsocket.GetInputVolume(string inputName)
    {
        return GetInputVolume(inputName);
    }

    public VolumeInfo GetInputVolume(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        var response = SendRequest(nameof(GetInputVolume), request);
        return new VolumeInfo(response);
    }

    public bool GetInputMute(string inputName)
    {
        var requestFields = new JObject
        {
            { nameof(inputName), inputName }
        };

        var response = SendRequest(nameof(GetInputMute), requestFields);
        bool inputMuted = Convert.ToBoolean(response["inputMuted"]);
        return inputMuted;
    }

    public void SetInputMute(string inputName, bool inputMuted)
    {
        var requestFields = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputMuted), inputMuted }
        };

        SendRequest(nameof(SetInputMute), requestFields);
    }

    public void ToggleInputMute(string inputName)
    {
        var requestFields = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(ToggleInputMute), requestFields);
    }

    public void SetSceneItemTransform(string sceneName, int sceneItemId, JObject sceneItemTransform)
    {
        var requestFields = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemTransform), sceneItemTransform }
        };

        SendRequest(nameof(SetSceneItemTransform), requestFields);
    }

    void IObsWebsocket.SetSceneItemTransform(string sceneName, int sceneItemId,
        SceneItemTransformInfo sceneItemTransform)
    {
        SetSceneItemTransform(sceneName, sceneItemId, sceneItemTransform);
    }

    public void SetSceneItemTransform(string sceneName, int sceneItemId, SceneItemTransformInfo sceneItemTransform)
    {
        SetSceneItemTransform(sceneName, sceneItemId, JObject.FromObject(sceneItemTransform));
    }

    public void SetCurrentSceneCollection(string sceneCollectionName)
    {
        var requestFields = new JObject
        {
            { nameof(sceneCollectionName), sceneCollectionName }
        };

        SendRequest(nameof(SetCurrentSceneCollection), requestFields);
    }

    public string GetCurrentSceneCollection()
    {
        var response = SendRequest(nameof(GetSceneCollectionList));
        var currentCollectionName = response["currentSceneCollectionName"];
        string name = currentCollectionName?.ToString() ?? "";
        return name;
    }

    public List<string> GetSceneCollectionList()
    {
        var response = SendRequest(nameof(GetSceneCollectionList));
        string collection = response["sceneCollections"]?.ToString() ?? "";
        List<string> sceneCollections = JsonConvert.DeserializeObject<List<string>>(collection) ?? new List<string>();
        return sceneCollections;
    }

    public void SetCurrentProfile(string profileName)
    {
        var requestFields = new JObject
        {
            { nameof(profileName), profileName }
        };

        SendRequest(nameof(SetCurrentProfile), requestFields);
    }

    GetProfileListInfo IObsWebsocket.GetProfileList()
    {
        return GetProfileList();
    }

    public GetProfileListInfo GetProfileList()
    {
        var response = SendRequest(nameof(GetProfileList));
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
        var response = SendRequest(nameof(StopRecord));
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
        var response = SendRequest(nameof(GetRecordDirectory));
        string recordDirectory = response["recordDirectory"]?.ToString() ?? "";
        return recordDirectory;
    }

    RecordingStatus IObsWebsocket.GetRecordStatus()
    {
        return GetRecordStatus();
    }

    public RecordingStatus GetRecordStatus()
    {
        var response = SendRequest(nameof(GetRecordStatus));
        RecordingStatus status = JsonConvert.DeserializeObject<RecordingStatus>(response.ToString()) ??
                                 new RecordingStatus();
        return status;
    }

    public bool GetReplayBufferStatus()
    {
        var response = SendRequest(nameof(GetReplayBufferStatus));
        bool outputActive = Convert.ToBoolean(response["outputActive"]);
        return outputActive;
    }

    GetTransitionListInfo IObsWebsocket.GetSceneTransitionList()
    {
        return GetSceneTransitionList();
    }

    public GetTransitionListInfo GetSceneTransitionList()
    {
        var response = SendRequest(nameof(GetSceneTransitionList));
        GetTransitionListInfo info = JsonConvert.DeserializeObject<GetTransitionListInfo>(response.ToString()) ?? new GetTransitionListInfo();
        return info;
    }

    public bool GetStudioModeEnabled()
    {
        var response = SendRequest(nameof(GetStudioModeEnabled));
        bool studioModeEnabled = Convert.ToBoolean(response["studioModeEnabled"]);
        return studioModeEnabled;
    }

    public void SetStudioModeEnabled(bool studioModeEnabled)
    {
        var requestFields = new JObject
        {
            { nameof(studioModeEnabled), studioModeEnabled }
        };

        SendRequest(nameof(SetStudioModeEnabled), requestFields);
    }

    public string GetCurrentPreviewScene()
    {
        var response = SendRequest(nameof(GetCurrentPreviewScene));
        string currentPreviewSceneName = response["currentPreviewSceneName"]?.ToString() ?? "";
        return currentPreviewSceneName;
    }

    public void SetCurrentPreviewScene(string sceneName)
    {
        var requestFields = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        SendRequest(nameof(SetCurrentPreviewScene), requestFields);
    }

    void IObsWebsocket.SetCurrentPreviewScene(ObsScene previewScene)
    {
        SetCurrentPreviewScene(previewScene);
    }

    public void SetCurrentPreviewScene(ObsScene previewScene)
    {
        if (string.IsNullOrEmpty(previewScene.Name)) return;
        SetCurrentPreviewScene(previewScene.Name);
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
        var requestFields = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputAudioSyncOffset), inputAudioSyncOffset }
        };

        SendRequest(nameof(SetInputAudioSyncOffset), requestFields);
    }

    public int GetInputAudioSyncOffset(string inputName)
    {
        var requestFields = new JObject
        {
            { nameof(inputName), inputName }
        };
        var response = SendRequest(nameof(GetInputAudioSyncOffset), requestFields);
        int inputAudioSyncOffset = Convert.ToInt32(response["inputAudioSyncOffset"]);
        return inputAudioSyncOffset;
    }

    public void RemoveSceneItem(string sceneName, int sceneItemId)
    {
        var requestFields = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        SendRequest(nameof(RemoveSceneItem), requestFields);
    }

    public void SendStreamCaption(string captionText)
    {
        var requestFields = new JObject
        {
            { nameof(captionText), captionText }
        };

        SendRequest(nameof(SendStreamCaption), requestFields);
    }

    public void DuplicateSceneItem(string sceneName, int sceneItemId, string destinationSceneName = "")
    {
        var requestFields = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        if (!string.IsNullOrEmpty(destinationSceneName))
        {
            requestFields.Add(nameof(destinationSceneName), destinationSceneName);
        }

        SendRequest(nameof(DuplicateSceneItem), requestFields);
    }

    public Dictionary<string, string> GetSpecialInputs()
    {
        var response = SendRequest(nameof(GetSpecialInputs));
        var sources = new Dictionary<string, string>();
        foreach (var (key, jToken) in response)
        {
            string value = (string)jToken ?? "";
            if (key != "requestType")
            {
                sources.Add(key, value);
            }
        }

        return sources;
    }

    void IObsWebsocket.SetStreamServiceSettings(StreamingService service)
    {
        SetStreamServiceSettings(service);
    }

    StreamingService IObsWebsocket.GetStreamServiceSettings()
    {
        return GetStreamServiceSettings();
    }

    public void SetStreamServiceSettings(StreamingService service)
    {
        if(service.Settings == null) return;
        var requestFields = new JObject
        {
            { "streamServiceType", service.Type },
            { "streamServiceSettings", JToken.FromObject(service.Settings) }
        };

        SendRequest(nameof(SetStreamServiceSettings), requestFields);
    }
    
    public StreamingService GetStreamServiceSettings()
    {
        var response = SendRequest(nameof(GetStreamServiceSettings));
        StreamingService service = JsonConvert.DeserializeObject<StreamingService>(response.ToString()) ??
                                   new StreamingService();
        return service;
    }

    public string GetInputAudioMonitorType(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        var response = SendRequest(nameof(GetInputAudioMonitorType), request);
        string monitorType = response["monitorType"]?.ToString() ?? "";
        return monitorType;
    }

    public void SetInputAudioMonitorType(string inputName, string monitorType)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(monitorType), monitorType }
        };

        SendRequest(nameof(SetInputAudioMonitorType), request);
    }

    public void BroadcastCustomEvent(JObject eventData)
    {
        var request = new JObject
        {
            { nameof(eventData), eventData }
        };

        SendRequest(nameof(BroadcastCustomEvent), request);
    }

    public void SetMediaInputCursor(string inputName, int mediaCursor)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(mediaCursor), mediaCursor }
        };

        SendRequest(nameof(SetMediaInputCursor), request);
    }

    public void OffsetMediaInputCursor(string inputName, int mediaCursorOffset)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(mediaCursorOffset), mediaCursorOffset }
        };

        SendRequest(nameof(OffsetMediaInputCursor), request);
    }
    
    public int CreateInput(string sceneName, string inputName, string inputKind, JObject inputSettings,
        bool? sceneItemEnabled)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(inputName), inputName },
            { nameof(inputKind), inputKind }
        };

        request.Add(nameof(inputSettings), inputSettings);

        if (sceneItemEnabled.HasValue)
        {
            request.Add(nameof(sceneItemEnabled), sceneItemEnabled.Value);
        }

        var response = SendRequest(nameof(CreateInput), request);
        int sceneItemId = Convert.ToInt32(response["sceneItemId"]);
        return sceneItemId;
    }
    
    public JObject GetInputDefaultSettings(string inputKind)
    {
        var request = new JObject
        {
            { nameof(inputKind), inputKind }
        };

        var response = SendRequest(nameof(GetInputDefaultSettings), request);
        JObject defaultInputSettings = (JObject)response["defaultInputSettings"] ?? new JObject();
        return defaultInputSettings;
    }

    List<SceneItemDetails> IObsWebsocket.GetSceneItemList(string sceneName)
    {
        return GetSceneItemList(sceneName);
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

        if (request == null)
            return new List<SceneItemDetails>();
        
        var response = SendRequest(nameof(GetSceneItemList), request);
        return response["sceneItems"]?.Select(m => new SceneItemDetails((JObject)m)).ToList() ?? new List<SceneItemDetails>();
    }

    public int CreateSceneItem(string sceneName, string sourceName, bool sceneItemEnabled = true)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sourceName), sourceName },
            { nameof(sceneItemEnabled), sceneItemEnabled }
        };

        var response = SendRequest(nameof(CreateSceneItem), request);
        int sceneItemId = Convert.ToInt32(response["sceneItemId"]);
        return sceneItemId;
    }

    public void CreateScene(string sceneName)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        SendRequest(nameof(CreateScene), request);
    }

    SourceTracks IObsWebsocket.GetInputAudioTracks(string inputName)
    {
        return GetInputAudioTracks(inputName);
    }

    public SourceTracks GetInputAudioTracks(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        var response = SendRequest(nameof(GetInputAudioTracks), request);
        return new SourceTracks(response);
    }

    public void SetInputAudioTracks(string inputName, JObject inputAudioTracks)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputAudioTracks), inputAudioTracks }
        };

        SendRequest(nameof(SetInputAudioTracks), request);
    }

    void IObsWebsocket.SetInputAudioTracks(string inputName, SourceTracks inputAudioTracks)
    {
        SetInputAudioTracks(inputName, inputAudioTracks);
    }

    SourceActiveInfo IObsWebsocket.GetSourceActive(string sourceName)
    {
        return GetSourceActive(sourceName);
    }

    VirtualCamStatus IObsWebsocket.GetVirtualCamStatus()
    {
        return GetVirtualCamStatus();
    }

    public void SetInputAudioTracks(string inputName, SourceTracks inputAudioTracks)
    {
        SetInputAudioTracks(inputName, JObject.FromObject(inputAudioTracks));
    }

    public SourceActiveInfo GetSourceActive(string sourceName)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName }
        };

        var response = SendRequest(nameof(GetSourceActive), request);
        return new SourceActiveInfo(response);
    }

    public VirtualCamStatus GetVirtualCamStatus()
    {
        JObject response = SendRequest(nameof(GetVirtualCamStatus));
        var outputStatus = new VirtualCamStatus(response);
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

    VirtualCamStatus IObsWebsocket.ToggleVirtualCam()
    {
        return ToggleVirtualCam();
    }

    public VirtualCamStatus ToggleVirtualCam()
    {
        JObject response = SendRequest(nameof(ToggleVirtualCam));
        var outputStatus = new VirtualCamStatus(response);
        return outputStatus;
    }

    public JObject GetPersistentData(string realm, string slotName)
    {
        var request = new JObject
        {
            { nameof(realm), realm },
            { nameof(slotName), slotName }
        };

        return SendRequest(nameof(GetPersistentData), request);
    }

    public void SetPersistentData(string realm, string slotName, JObject slotValue)
    {
        var request = new JObject
        {
            { nameof(realm), realm },
            { nameof(slotName), slotName },
            { nameof(slotValue), slotValue }
        };

        SendRequest(nameof(SetPersistentData), request);
    }

    public void CreateSceneCollection(string sceneCollectionName)
    {
        var request = new JObject
        {
            { nameof(sceneCollectionName), sceneCollectionName }
        };

        SendRequest(nameof(CreateSceneCollection), request);
    }

    public void CreateProfile(string profileName)
    {
        var request = new JObject
        {
            { nameof(profileName), profileName }
        };

        SendRequest(nameof(CreateProfile), request);
    }

    public void RemoveProfile(string profileName)
    {
        var request = new JObject
        {
            { nameof(profileName), profileName }
        };

        SendRequest(nameof(RemoveProfile), request);
    }

    public JObject GetProfileParameter(string parameterCategory, string parameterName)
    {
        var request = new JObject
        {
            { nameof(parameterCategory), parameterCategory },
            { nameof(parameterName), parameterName }
        };

        return SendRequest(nameof(GetProfileParameter), request);
    }

    public void SetProfileParameter(string parameterCategory, string parameterName, string parameterValue)
    {
        var request = new JObject
        {
            { nameof(parameterCategory), parameterCategory },
            { nameof(parameterName), parameterName },
            { nameof(parameterValue), parameterValue }
        };

        SendRequest(nameof(SetProfileParameter), request);
    }

    void IObsWebsocket.SetVideoSettings(ObsVideoSettings obsVideoSettings)
    {
        SetVideoSettings(obsVideoSettings);
    }

    public void SetVideoSettings(ObsVideoSettings obsVideoSettings)
    {
        SendRequest(nameof(SetVideoSettings), JObject.FromObject(obsVideoSettings));
    }

    public JObject GetSourceFilterDefaultSettings(string filterKind)
    {
        var request = new JObject
        {
            { nameof(filterKind), filterKind }
        };

        return SendRequest(nameof(GetSourceFilterDefaultSettings), request);
    }

    public void SetSourceFilterName(string sourceName, string filterName, string newFilterName)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(newFilterName), newFilterName }
        };

        SendRequest(nameof(SetSourceFilterName), request);
    }

    public void SetSourceFilterIndex(string sourceName, string filterName, int filterIndex)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName },
            { nameof(filterIndex), filterIndex }
        };

        SendRequest(nameof(SetSourceFilterIndex), request);
    }

    ObsVersion IObsWebsocket.GetVersion()
    {
        return GetVersion();
    }

    public ObsVersion GetVersion()
    {
        JObject response = SendRequest(nameof(GetVersion));
        return new ObsVersion(response);
    }

    public JObject CallVendorRequest(string vendorName, string requestType, JObject requestData = null)
    {
        var request = new JObject
        {
            { nameof(vendorName), vendorName },
            { nameof(requestType), requestType },
            { nameof(requestData), requestData }
        };

        return SendRequest(nameof(CallVendorRequest), request);
    }

    public List<string> GetHotkeyList()
    {
        var response = SendRequest(nameof(GetHotkeyList));
        string hotkeys = response["hotkeys"]?.ToString() ?? "";
        List<string> hotkeysList = JsonConvert.DeserializeObject<List<string>>(hotkeys) ?? new List<string>();
        return hotkeysList;
    }

    public void Sleep(int sleepMillis, int sleepFrames)
    {
        var request = new JObject
        {
            { nameof(sleepMillis), sleepMillis },
            { nameof(sleepFrames), sleepFrames }
        };

        SendRequest(nameof(Sleep), request);
    }

    List<InputBasicInfo> IObsWebsocket.GetInputList(string inputKind)
    {
        return GetInputList(inputKind);
    }

    public List<InputBasicInfo> GetInputList(string inputKind = "")
    {
        var request = new JObject
        {
            { nameof(inputKind), inputKind }
        };

        var response = SendRequest(nameof(GetInputList), request);

        JToken inputs = response["inputs"];
        if (inputs == null) return new List<InputBasicInfo>();
        
        var returnList = new List<InputBasicInfo>();
        foreach (var input in inputs)
        {
            returnList.Add(new InputBasicInfo((JObject)input));
        }

        return returnList;
    }

    public List<string> GetInputKindList(bool unversioned = false)
    {
        var request = new JObject
        {
            { nameof(unversioned), unversioned }
        };

        var response = unversioned is false
            ? SendRequest(nameof(GetInputKindList))
            : SendRequest(nameof(GetInputKindList), request);

        string inputKinds = response["inputKinds"]?.ToString() ?? "";
        List<string> inputKindList = JsonConvert.DeserializeObject<List<string>>(inputKinds) ?? new List<string>();
        return inputKindList;
    }

    public void RemoveInput(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(RemoveInput), request);
    }

    public void SetInputName(string inputName, string newInputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(newInputName), newInputName }
        };

        SendRequest(nameof(SetInputName), request);
    }

    InputSettings IObsWebsocket.GetInputSettings(string inputName)
    {
        return GetInputSettings(inputName);
    }

    void IObsWebsocket.SetInputSettings(InputSettings inputSettings, bool overlay)
    {
        SetInputSettings(inputSettings, overlay);
    }

    public InputSettings GetInputSettings(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        var response = SendRequest(nameof(GetInputSettings), request);
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
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputSettings), inputSettings },
            { nameof(overlay), overlay }
        };

        SendRequest(nameof(SetInputSettings), request);
    }

    public double GetInputAudioBalance(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        var response = SendRequest(nameof(GetInputAudioBalance), request);
        double inputAudioBalance = Convert.ToDouble(response["inputAudioBalance"]);
        return inputAudioBalance;
    }

    public void SetInputAudioBalance(string inputName, double inputAudioBalance)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(inputAudioBalance), inputAudioBalance }
        };

        SendRequest(nameof(SetInputAudioBalance), request);
    }

    public List<JObject> GetInputPropertiesListPropertyItems(string inputName, string propertyName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(propertyName), propertyName }
        };

        var response = SendRequest(nameof(GetInputPropertiesListPropertyItems), request);
        List<JObject> propertyItems = response["propertyItems"]?.Value<List<JObject>>() ?? new List<JObject>();
        return propertyItems;
    }

    public void PressInputPropertiesButton(string inputName, string propertyName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(propertyName), propertyName }
        };

        SendRequest(nameof(PressInputPropertiesButton), request);
    }

    MediaInputStatus IObsWebsocket.GetMediaInputStatus(string inputName)
    {
        return GetMediaInputStatus(inputName);
    }

    public MediaInputStatus GetMediaInputStatus(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        return new MediaInputStatus(SendRequest(nameof(GetMediaInputStatus), request));
    }

    public void TriggerMediaInputAction(string inputName, string mediaAction)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(mediaAction), mediaAction }
        };

        SendRequest(nameof(TriggerMediaInputAction), request);
    }

    public string GetLastReplayBufferReplay()
    {
        var response = SendRequest(nameof(GetLastReplayBufferReplay));
        string savedReplayPath = response["savedReplayPath"]?.ToString() ?? "";
        return savedReplayPath;
    }

    public void ToggleRecordPause()
    {
        SendRequest(nameof(ToggleRecordPause));
    }

    public List<JObject> GetGroupSceneItemList(string sceneName)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        var response = SendRequest(nameof(GetGroupSceneItemList), request);
        string sceneItems = response["sceneItems"]?.ToString() ?? "";
        List<JObject> sceneItemObjects = JsonConvert.DeserializeObject<List<JObject>>(sceneItems) ?? new List<JObject>();
        return sceneItemObjects;
    }

    public int GetSceneItemId(string sceneName, string sourceName, int searchOffset)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sourceName), sourceName },
            { nameof(searchOffset), searchOffset }
        };

        var response = SendRequest(nameof(GetSceneItemId), request);
        int sceneItemId = Convert.ToInt32(response["sceneItemId"]);
        return sceneItemId;
    }

    SceneItemTransformInfo IObsWebsocket.GetSceneItemTransform(string sceneName, int sceneItemId)
    {
        return GetSceneItemTransform(sceneName, sceneItemId);
    }

    public SceneItemTransformInfo GetSceneItemTransform(string sceneName, int sceneItemId)
    {
        var response = GetSceneItemTransformRaw(sceneName, sceneItemId);
        string sceneItemTransform = response["sceneItemTransform"]?.ToString() ?? "";
        SceneItemTransformInfo info = JsonConvert.DeserializeObject<SceneItemTransformInfo>(sceneItemTransform) ?? new SceneItemTransformInfo();
        return info;
    }

    public JObject GetSceneItemTransformRaw(string sceneName, int sceneItemId)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        return SendRequest(nameof(GetSceneItemTransform), request);
    }

    public bool GetSceneItemEnabled(string sceneName, int sceneItemId)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        var response = SendRequest(nameof(GetSceneItemEnabled), request);
        bool sceneItemEnabled = Convert.ToBoolean(response["sceneItemEnabled"]);
        return sceneItemEnabled;
    }

    public void SetSceneItemEnabled(string sceneName, int sceneItemId, bool sceneItemEnabled)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemEnabled), sceneItemEnabled }
        };

        SendRequest(nameof(SetSceneItemEnabled), request);
    }

    public bool GetSceneItemLocked(string sceneName, int sceneItemId)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        var response = SendRequest(nameof(GetSceneItemLocked), request);
        bool sceneItemLocked = Convert.ToBoolean(response["sceneItemLocked"]);
        return sceneItemLocked;
    }

    public void SetSceneItemLocked(string sceneName, int sceneItemId, bool sceneItemLocked)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemLocked), sceneItemLocked }
        };

        SendRequest(nameof(SetSceneItemLocked), request);
    }

    public int GetSceneItemIndex(string sceneName, int sceneItemId)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        var response = SendRequest(nameof(GetSceneItemIndex), request);
        int sceneItemIndex = Convert.ToInt32(response["sceneItemIndex"]);
        return sceneItemIndex;
    }

    public void SetSceneItemIndex(string sceneName, int sceneItemId, int sceneItemIndex)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemIndex), sceneItemIndex }
        };

        SendRequest(nameof(SetSceneItemIndex), request);
    }

    public string GetSceneItemBlendMode(string sceneName, int sceneItemId)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        var response = SendRequest(nameof(GetSceneItemBlendMode), request);
        string sceneItemBlendMode = response["sceneItemBlendMode"]?.ToString() ?? "";
        return sceneItemBlendMode;
    }

    public void SetSceneItemBlendMode(string sceneName, int sceneItemId, string sceneItemBlendMode)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId },
            { nameof(sceneItemBlendMode), sceneItemBlendMode }
        };

        SendRequest(nameof(SetSceneItemBlendMode), request);
    }

    public List<string> GetGroupList()
    {
        var response = SendRequest(nameof(GetGroupList));
        string groupName = response["groups"]?.ToString() ?? "";
        List<string> groups = JsonConvert.DeserializeObject<List<string>>(groupName) ?? new List<string>();
        return groups;
    }

    public void RemoveScene(string sceneName)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        SendRequest(nameof(RemoveScene), request);
    }

    public void SetSceneName(string sceneName, string newSceneName)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(newSceneName), newSceneName }
        };

        SendRequest(nameof(SetSceneName), request);
    }

    public string GetSourceScreenshot(string sourceName, string imageFormat, int imageWidth = -1, int imageHeight = -1,
        int imageCompressionQuality = -1)
    {
        var request = new JObject
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

        var response = SendRequest(nameof(GetSourceScreenshot), request);
        string imageData = response["imageData"]?.ToString() ?? "";
        return imageData;
    }

    public List<string> GetTransitionKindList()
    {
        var response = SendRequest(nameof(GetTransitionKindList));
        string transitionKinds = response["transitionKinds"]?.ToString() ?? "";
        List<string> transitionKindList = JsonConvert.DeserializeObject<List<string>>(transitionKinds) ?? new List<string>();
        return transitionKindList;
    }

    public double GetCurrentSceneTransitionCursor()
    {
        var response = SendRequest(nameof(GetCurrentSceneTransitionCursor));
        double transitionCursor = Convert.ToDouble(response["transitionCursor"]);
        return transitionCursor;
    }

    public void OpenInputPropertiesDialog(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(OpenInputPropertiesDialog), request);
    }

    public void OpenInputFiltersDialog(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(OpenInputFiltersDialog), request);
    }

    public void OpenInputInteractDialog(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        SendRequest(nameof(OpenInputInteractDialog), request);
    }

    public List<Monitor> GetMonitorList()
    {
        var response = SendRequest(nameof(GetMonitorList));
        var monitors = new List<Monitor>();
        var monitorObj = response["monitors"];
        if (monitorObj == null) return new List<Monitor>();
        
        foreach (var monitor in monitorObj)
        {
            monitors.Add(new Monitor((JObject)monitor));
        }

        return monitors;
    }

    public void OpenSourceProjector(string sourceName, string projectorGeometry, int monitorIndex = -1)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(projectorGeometry), projectorGeometry },
            { nameof(monitorIndex), monitorIndex },
        };

        SendRequest(nameof(OpenSourceProjector), request);
    }

    public void OpenVideoMixProjector(string videoMixType, string projectorGeometry, int monitorIndex = -1)
    {
        var request = new JObject
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
        string profiles;
        string sceneName;
        string sceneItems;
        string scenes;
        string sceneCollectionName;
        string sceneItemIdx;
        string sceneCollections;
        string sourceName;
        string transitionName;
        string inputName;
        string inputKind;
        string filterName;
        string filterKind;
        string filterObj;
        string mediaAction;
        string monitorType;
        
        int filterIndex;
        int sceneItemId;
        int sceneItemIndex;
        int transitionDuration;
        int inputAudioSyncOffset;

        bool sceneItemEnabled;
        bool studioModeEnabled;
        bool inputMuted;
        bool filterEnabled;
        bool isGroup;
        
        switch (eventType)
        {
            case nameof(CurrentProgramSceneChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                CurrentProgramSceneChanged.Invoke(this, new ProgramSceneChangedEventArgs(sceneName));
                break;
            case nameof(SceneListChanged):
                scenes = bodyObj["scenes"]?.ToString() ?? "";
                List<JObject> sceneList = JsonConvert.DeserializeObject<List<JObject>>(scenes) ?? new List<JObject>();
                SceneListChanged.Invoke(this,
                    new SceneListChangedEventArgs(sceneList));
                break;
            case nameof(SceneItemListReindexed):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItems = bodyObj["sceneItems"]?.ToString() ?? "";
                List<JObject> sceneItemsList =
                    JsonConvert.DeserializeObject<List<JObject>>(sceneItems) ?? new List<JObject>();
                SceneItemListReindexed.Invoke(this,
                    new SceneItemListReindexedEventArgs(sceneName, sceneItemsList));
                break;
            case nameof(SceneItemCreated):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                sceneItemId = Convert.ToInt32(bodyObj["sceneItemId"]);
                sceneItemIndex = Convert.ToInt32(bodyObj["sceneItemIndex"]);
                SceneItemCreated.Invoke(this,
                    new SceneItemCreatedEventArgs(sceneName, sourceName, sceneItemId, sceneItemIndex));
                break;
            case nameof(SceneItemRemoved):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                sceneItemId = Convert.ToInt32(bodyObj["sceneItemId"]);

                SceneItemRemoved.Invoke(this,
                    new SceneItemRemovedEventArgs(sceneName, sourceName, sceneItemId));
                break;
            case nameof(SceneItemEnableStateChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItemId = Convert.ToInt32(bodyObj["sceneItemId"]);
                sceneItemEnabled = Convert.ToBoolean(bodyObj["sceneItemEnabled"]);

                SceneItemEnableStateChanged.Invoke(this,
                    new SceneItemEnableStateChangedEventArgs(sceneName, sceneItemId, sceneItemEnabled));
                break;
            case nameof(SceneItemLockStateChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItemId = Convert.ToInt32(bodyObj["sceneItemId"]);
                sceneItemEnabled = Convert.ToBoolean(bodyObj["sceneItemEnabled"]);

                SceneItemLockStateChanged.Invoke(this,
                    new SceneItemLockStateChangedEventArgs(sceneName, sceneItemId, sceneItemEnabled));
                break;
            case nameof(CurrentSceneCollectionChanged):
                sceneCollectionName = bodyObj["sceneCollectionName"]?.ToString() ?? "";
                CurrentSceneCollectionChanged.Invoke(this,
                    new CurrentSceneCollectionChangedEventArgs(sceneCollectionName));
                break;
            case nameof(SceneCollectionListChanged):
                sceneCollections = bodyObj["sceneCollections"]?.ToString() ?? "";
                List<string> sceneCollectionsList = JsonConvert.DeserializeObject<List<string>>(sceneCollections) ?? new List<string>();

                SceneCollectionListChanged.Invoke(this,
                    new SceneCollectionListChangedEventArgs(sceneCollectionsList));
                break;
            case nameof(CurrentSceneTransitionChanged):
                transitionName = bodyObj["transitionName"]?.ToString() ?? "";
                CurrentSceneTransitionChanged.Invoke(this,
                    new CurrentSceneTransitionChangedEventArgs(transitionName));
                break;
            case nameof(CurrentSceneTransitionDurationChanged):
                transitionDuration = Convert.ToInt32(bodyObj["transitionDuration"]);
                CurrentSceneTransitionDurationChanged.Invoke(this,
                    new CurrentSceneTransitionDurationChangedEventArgs(transitionDuration));
                break;
            case nameof(SceneTransitionStarted):
                transitionName = bodyObj["transitionName"]?.ToString() ?? "";
                SceneTransitionStarted.Invoke(this,
                    new SceneTransitionStartedEventArgs(transitionName));
                break;
            case nameof(SceneTransitionEnded):
                transitionName = bodyObj["transitionName"]?.ToString() ?? "";
                SceneTransitionEnded.Invoke(this, new SceneTransitionEndedEventArgs(transitionName));
                break;
            case nameof(SceneTransitionVideoEnded):
                transitionName = bodyObj["transitionName"]?.ToString() ?? "";
                SceneTransitionVideoEnded.Invoke(this,
                    new SceneTransitionVideoEndedEventArgs(transitionName));
                break;
            case nameof(CurrentProfileChanged):
                profileName = bodyObj["profileName"]?.ToString() ?? "";
                CurrentProfileChanged.Invoke(this, new CurrentProfileChangedEventArgs(profileName));
                break;
            case nameof(ProfileListChanged):
                profiles = bodyObj["profiles"]?.ToString() ?? "";
                List<string> profileList = JsonConvert.DeserializeObject<List<string>>(profiles) ?? new List<string>();
                ProfileListChanged.Invoke(this, new ProfileListChangedEventArgs(profileList));
                break;
            case nameof(StreamStateChanged):
                StreamStateChanged?.Invoke(this, new StreamStateChangedEventArgs(new OutputStateChanged(body)));
                break;
            case nameof(RecordStateChanged):
                RecordStateChanged.Invoke(this, new RecordStateChangedEventArgs(new RecordStateChanged(body)));
                break;
            case nameof(CurrentPreviewSceneChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                
                CurrentPreviewSceneChanged.Invoke(this, new CurrentPreviewSceneChangedEventArgs(sceneName));
                break;
            case nameof(StudioModeStateChanged):
                studioModeEnabled = Convert.ToBoolean(bodyObj["studioModeEnabled"]);
                StudioModeStateChanged.Invoke(this,
                    new StudioModeStateChangedEventArgs(studioModeEnabled));
                break;
            case nameof(ReplayBufferStateChanged):
                ReplayBufferStateChanged.Invoke(this,
                    new ReplayBufferStateChangedEventArgs(new OutputStateChanged(body)));
                break;
            case nameof(ExitStarted):
                ExitStarted.Invoke(this, EventArgs.Empty);
                break;
            case nameof(SceneItemSelected):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItemIdx = bodyObj["sceneItemId"]?.ToString() ?? "";
                SceneItemSelected.Invoke(this, new SceneItemSelectedEventArgs(sceneName, sceneItemIdx));
                break;
            case nameof(SceneItemTransformChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                sceneItemIdx = bodyObj["sceneItemId"]?.ToString() ?? "";
                JObject sceneItemTransform = (JObject)bodyObj["sceneItemTransform"] ?? new JObject();
                SceneItemTransformChanged.Invoke(this,
                    new SceneItemTransformEventArgs(sceneName, sceneItemIdx,
                        new SceneItemTransformInfo(sceneItemTransform)));
                break;
            case nameof(InputAudioSyncOffsetChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                inputAudioSyncOffset = Convert.ToInt32(bodyObj["inputAudioSyncOffset"]);
                InputAudioSyncOffsetChanged.Invoke(this,
                    new InputAudioSyncOffsetChangedEventArgs(inputName, inputAudioSyncOffset));
                break;
            case nameof(InputMuteStateChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                inputMuted = Convert.ToBoolean(bodyObj["inputMuted"]);
                InputMuteStateChanged.Invoke(this,
                    new InputMuteStateChangedEventArgs(inputName, inputMuted));
                break;
            case nameof(InputVolumeChanged):
                InputVolumeChanged.Invoke(this, new InputVolumeChangedEventArgs(new InputVolume(body)));
                break;
            case nameof(SourceFilterCreated):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                filterName = bodyObj["filterName"]?.ToString() ?? "";
                filterKind = bodyObj["filterKind"]?.ToString() ?? "";
                filterIndex = Convert.ToInt32(bodyObj["filterIndex"]);
                JObject filterSettings = (JObject)bodyObj["filterSettings"] ?? new JObject();
                JObject defaultFilterSettings = (JObject)bodyObj["defaultFilterSettings"] ?? new JObject();
                SourceFilterCreated.Invoke(this,
                    new SourceFilterCreatedEventArgs(sourceName, filterName,
                        filterKind, filterIndex, filterSettings, defaultFilterSettings));
                break;
            case nameof(SourceFilterRemoved):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                filterName = bodyObj["filterName"]?.ToString() ?? "";
                
                SourceFilterRemoved.Invoke(this,
                    new SourceFilterRemovedEventArgs(sourceName, filterName));
                break;
            case nameof(SourceFilterListReindexed):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                filterObj = bodyObj["filters"]?.ToString() ?? "";
                
                List<FilterReorderItem> filters = new List<FilterReorderItem>();
                JsonConvert.PopulateObject(filterObj, filters);

                SourceFilterListReindexed.Invoke(this,
                    new SourceFilterListReindexedEventArgs(sourceName, filters));
                
                break;
            case nameof(SourceFilterEnableStateChanged):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                filterName = bodyObj["filterName"]?.ToString() ?? "";
                filterEnabled = Convert.ToBoolean(bodyObj["filterEnabled"]);
                SourceFilterEnableStateChanged.Invoke(this,
                    new SourceFilterEnableStateChangedEventArgs(sourceName, filterName, filterEnabled));
                break;
            case nameof(VendorEvent):
                string vendorName = bodyObj["vendorName"]?.ToString() ?? "";
                string @event = bodyObj["event"]?.ToString() ?? "";
                VendorEvent.Invoke(this,
                    new VendorEventArgs(vendorName, @event, body));
                break;
            case nameof(MediaInputPlaybackEnded):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                MediaInputPlaybackEnded.Invoke(this, new MediaInputPlaybackEndedEventArgs(inputName));
                break;
            case nameof(MediaInputPlaybackStarted):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                MediaInputPlaybackStarted.Invoke(this,
                    new MediaInputPlaybackStartedEventArgs(sourceName));
                break;
            case nameof(MediaInputActionTriggered):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                mediaAction = bodyObj["mediaAction"]?.ToString() ?? "";
                MediaInputActionTriggered.Invoke(this,
                    new MediaInputActionTriggeredEventArgs(inputName, mediaAction));
                break;
            case nameof(VirtualcamStateChanged):
                VirtualcamStateChanged.Invoke(this, new VirtualcamStateChangedEventArgs(new OutputStateChanged(body)));
                break;
            case nameof(CurrentSceneCollectionChanging):
                sceneCollectionName = bodyObj["sceneCollectionName"]?.ToString() ?? "";
                CurrentSceneCollectionChanging.Invoke(this,
                    new CurrentSceneCollectionChangingEventArgs(sceneCollectionName));
                break;
            case nameof(CurrentProfileChanging):
                profileName = bodyObj["profileName"]?.ToString() ?? "";
                CurrentProfileChanging.Invoke(this, new CurrentProfileChangingEventArgs(profileName));
                break;
            case nameof(SourceFilterNameChanged):
                sourceName = bodyObj["sourceName"]?.ToString() ?? "";
                string oldFilterName = bodyObj["oldFilterName"]?.ToString() ?? "";
                filterName = bodyObj["filterName"]?.ToString() ?? "";
                SourceFilterNameChanged.Invoke(this,
                    new SourceFilterNameChangedEventArgs(sourceName, oldFilterName, filterName));
                break;
            case nameof(InputCreated):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                inputKind = bodyObj["inputKind"]?.ToString() ?? "";
                string unversionedInputKind = bodyObj["unversionedInputKind"]?.ToString() ?? "";
                JObject inputSettings = (JObject)bodyObj["inputSettings"] ?? new JObject();
                JObject defaultInputSettings = (JObject)bodyObj["defaultInputSettings"] ?? new JObject();
                InputCreated.Invoke(this,
                    new InputCreatedEventArgs(inputName, inputKind, unversionedInputKind, 
                        inputSettings, defaultInputSettings));
                break;
            case nameof(InputRemoved):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                InputRemoved.Invoke(this, new InputRemovedEventArgs(inputName));
                break;
            case nameof(InputNameChanged):
                string oldInputName = bodyObj["oldInputName"]?.ToString() ?? "";
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                InputNameChanged.Invoke(this,
                    new InputNameChangedEventArgs(oldInputName, inputName));
                break;
            case nameof(InputActiveStateChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                bool videoActive = Convert.ToBoolean(bodyObj["videoActive"]);
                InputActiveStateChanged.Invoke(this,
                    new InputActiveStateChangedEventArgs(inputName, videoActive));
                break;
            case nameof(InputShowStateChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                bool videoShowing = Convert.ToBoolean(bodyObj["videoShowing"]);

                InputShowStateChanged.Invoke(this,
                    new InputShowStateChangedEventArgs(inputName, videoShowing));
                break;
            case nameof(InputAudioBalanceChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                double inputAudioBalance = Convert.ToDouble(body["inputAudioBalance"]);
                InputAudioBalanceChanged.Invoke(this,
                    new InputAudioBalanceChangedEventArgs(inputName, inputAudioBalance));
                break;
            case nameof(InputAudioTracksChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                JObject inputAudioTrack = (JObject)bodyObj["inputAudioTracks"] ?? new JObject();
                InputAudioTracksChanged.Invoke(this,
                    new InputAudioTracksChangedEventArgs(inputName, inputAudioTrack));
                break;
            case nameof(InputAudioMonitorTypeChanged):
                inputName = bodyObj["inputName"]?.ToString() ?? "";
                monitorType = bodyObj["monitorType"]?.ToString() ?? "";
                
                InputAudioMonitorTypeChanged.Invoke(this,
                    new InputAudioMonitorTypeChangedEventArgs(inputName, monitorType));
                break;
            case nameof(InputVolumeMeters):
                string inputs = bodyObj["inputs"]?.ToString() ?? "";
                List<JObject> inputList = JsonConvert.DeserializeObject<List<JObject>>(inputs) ?? new List<JObject>();
                InputVolumeMeters.Invoke(this,
                    new InputVolumeMetersEventArgs(inputList));
                break;
            case nameof(ReplayBufferSaved):
                string savedReplayPath = bodyObj["savedReplayPath"]?.ToString() ?? "";
                ReplayBufferSaved.Invoke(this, new ReplayBufferSavedEventArgs(savedReplayPath));
                break;
            case nameof(SceneCreated):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                isGroup = Convert.ToBoolean(bodyObj["isGroup"]);
                
                SceneCreated.Invoke(this, new SceneCreatedEventArgs(sceneName, isGroup));
                break;
            case nameof(SceneRemoved):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                isGroup = Convert.ToBoolean(bodyObj["isGroup"]);
                SceneRemoved.Invoke(this, new SceneRemovedEventArgs(sceneName, isGroup));
                break;
            case nameof(SceneNameChanged):
                sceneName = bodyObj["sceneName"]?.ToString() ?? "";
                string oldSceneName = bodyObj["oldSceneName"]?.ToString() ?? "";
                SceneNameChanged.Invoke(this,
                    new SceneNameChangedEventArgs(oldSceneName, sceneName));
                break;
            default:
                var message = $"Unsupported Event: {eventType}\n{body}";
                Console.WriteLine(message);
                Debug.WriteLine(message);
                break;
        }
    }
}