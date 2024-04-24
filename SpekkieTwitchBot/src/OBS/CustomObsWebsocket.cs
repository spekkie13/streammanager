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

namespace SpekkieTwitchBot.OBS;

public class CustomObsWebsocket : IObsWebsocket
{
    private const string WEBSOCKET_URL_PREFIX = "ws://";
    private const int SUPPORTED_RPC_VERSION = 1;
    private TimeSpan wsTimeout = TimeSpan.FromSeconds(10);
    private string connectionPassword;
    private WebsocketClient wsConnection;
    private delegate void RequestCallback(CustomObsWebsocket sender, JObject body);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> responseHandlers;
    private static readonly Random random = new Random();
    
    public TimeSpan WsTimeout
    {
        get { return wsConnection.ReconnectTimeout ?? wsTimeout; }
        set
        {
            wsTimeout = value;

            if (wsConnection != null)
            {
                wsConnection.ReconnectTimeout = wsTimeout;
            }
        }
    }

    public bool IsConnected
    {
        get { return (wsConnection != null && wsConnection.IsRunning); }
    }

    public CustomObsWebsocket()
    {
        responseHandlers = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();
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
        if (!url.ToLower().StartsWith(WEBSOCKET_URL_PREFIX))
        {
            throw new ArgumentException($"Invalid url, must start with '{WEBSOCKET_URL_PREFIX}'");
        }

        if (wsConnection != null && wsConnection.IsRunning)
        {
            Disconnect();
        }

        wsConnection = new WebsocketClient(new Uri(url));
        wsConnection.IsReconnectionEnabled = false;
        wsConnection.ReconnectTimeout = null;
        wsConnection.ErrorReconnectTimeout = null;
        wsConnection.MessageReceived.Subscribe(m => Task.Run(() => WebsocketMessageHandler(this, m)));
        wsConnection.DisconnectionHappened.Subscribe(d => Task.Run(() => OnWebsocketDisconnect(this, d)));

        connectionPassword = password;
        wsConnection.StartOrFail();
    }

    public void Disconnect()
    {
        connectionPassword = null;
        if (wsConnection != null)
        {
            try
            {
                wsConnection.Stop(WebSocketCloseStatus.NormalClosure, "User requested disconnect");
                ((IDisposable)wsConnection).Dispose();
            }
            catch
            {
            }

            wsConnection = null;
        }

        var unusedHandlers = responseHandlers.ToArray();
        responseHandlers.Clear();
        foreach (var cb in unusedHandlers)
        {
            var tcs = cb.Value;
            tcs.TrySetCanceled();
        }
    }

    private void OnWebsocketDisconnect(object sender, DisconnectionInfo d)
    {
        if (d == null || d.CloseStatus == null)
        {
            Disconnected?.Invoke(sender, new ObsDisconnectionInfo(ObsCloseCodes.UnknownReason, null, d));
        }
        else
        {
            Disconnected?.Invoke(sender,
                new ObsDisconnectionInfo((ObsCloseCodes)d.CloseStatus, d.CloseStatusDescription, d));
        }
    }

    private void WebsocketMessageHandler(object sender, ResponseMessage e)
    {
        if (e.MessageType != WebSocketMessageType.Text)
        {
            return;
        }

        ServerMessage msg = JsonConvert.DeserializeObject<ServerMessage>(e.Text);
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
                if (body.ContainsKey("requestId"))
                {
                    string msgID = (string)body["requestId"];

                    if (responseHandlers.TryRemove(msgID, out TaskCompletionSource<JObject> handler))
                    {
                        handler.SetResult(body);
                    }
                }

                break;
            case MessageTypes.Event:
                string eventType = body["eventType"].ToString();
                Task.Run(() => { ProcessEventType(eventType, body); });
                break;
        }
    }

    public JObject SendRequest(string requestType, JObject additionalFields = null)
    {
        return SendRequest(MessageTypes.Request, requestType, additionalFields);
    }

    internal JObject SendRequest(MessageTypes operationCode, string requestType, JObject additionalFields = null,
        bool waitForReply = true)
    {
        if (wsConnection == null)
        {
            throw new NullReferenceException("Websocket is not initialized");
        }

        var tcs = new TaskCompletionSource<JObject>();
        JObject message = null;
        do
        {
            message = MessageFactory.BuildMessage(operationCode, requestType, additionalFields, out string messageId);
            if (!waitForReply || responseHandlers.TryAdd(messageId, tcs))
            {
                break;
            }
        } while (true);

        wsConnection.Send(message.ToString());
        if (!waitForReply)
        {
            return null;
        }

        tcs.Task.Wait(wsTimeout.Milliseconds);

        if (tcs.Task.IsCanceled)
            throw new ErrorResponseException("Request canceled", 0);

        var result = tcs.Task.Result;

        if (!(bool)result["requestStatus"]["result"])
        {
            var status = (JObject)result["requestStatus"];
            throw new ErrorResponseException(
                $"ErrorCode: {status["code"]}{(status.ContainsKey("comment") ? $", Comment: {status["comment"]}" : "")}",
                (int)status["code"]);
        }

        if (result.ContainsKey("responseData")) // ResponseData is optional
            return result["responseData"].ToObject<JObject>();

        return new JObject();
    }

    public ObsAuthInfo GetAuthInfo()
    {
        JObject response = SendRequest("GetAuthRequired");
        return new ObsAuthInfo(response);
    }

    public event EventHandler<ProgramSceneChangedEventArgs>? CurrentProgramSceneChanged;
    public event EventHandler<SceneListChangedEventArgs>? SceneListChanged;
    public event EventHandler<SceneItemListReindexedEventArgs>? SceneItemListReindexed;
    public event EventHandler<SceneItemCreatedEventArgs>? SceneItemCreated;
    public event EventHandler<SceneItemRemovedEventArgs>? SceneItemRemoved;
    public event EventHandler<SceneItemEnableStateChangedEventArgs>? SceneItemEnableStateChanged;
    public event EventHandler<SceneItemLockStateChangedEventArgs>? SceneItemLockStateChanged;
    public event EventHandler<CurrentSceneCollectionChangedEventArgs>? CurrentSceneCollectionChanged;
    public event EventHandler<SceneCollectionListChangedEventArgs>? SceneCollectionListChanged;
    public event EventHandler<CurrentSceneTransitionChangedEventArgs>? CurrentSceneTransitionChanged;
    public event EventHandler<CurrentSceneTransitionDurationChangedEventArgs>? CurrentSceneTransitionDurationChanged;
    public event EventHandler<SceneTransitionStartedEventArgs>? SceneTransitionStarted;
    public event EventHandler<SceneTransitionEndedEventArgs>? SceneTransitionEnded;
    public event EventHandler<SceneTransitionVideoEndedEventArgs>? SceneTransitionVideoEnded;
    public event EventHandler<CurrentProfileChangedEventArgs>? CurrentProfileChanged;
    public event EventHandler<ProfileListChangedEventArgs>? ProfileListChanged;
    public event EventHandler<StreamStateChangedEventArgs>? StreamStateChanged;
    public event EventHandler<RecordStateChangedEventArgs>? RecordStateChanged;
    public event EventHandler<ReplayBufferStateChangedEventArgs>? ReplayBufferStateChanged;
    public event EventHandler<CurrentPreviewSceneChangedEventArgs>? CurrentPreviewSceneChanged;
    public event EventHandler<StudioModeStateChangedEventArgs>? StudioModeStateChanged;
    public event EventHandler? ExitStarted;
    public event EventHandler? Connected;
    public event EventHandler<ObsDisconnectionInfo>? Disconnected;
    public event EventHandler<SceneItemSelectedEventArgs>? SceneItemSelected;
    public event EventHandler<SceneItemTransformEventArgs>? SceneItemTransformChanged;
    public event EventHandler<InputAudioSyncOffsetChangedEventArgs>? InputAudioSyncOffsetChanged;
    public event EventHandler<SourceFilterCreatedEventArgs>? SourceFilterCreated;
    public event EventHandler<SourceFilterRemovedEventArgs>? SourceFilterRemoved;
    public event EventHandler<SourceFilterListReindexedEventArgs>? SourceFilterListReindexed;
    public event EventHandler<SourceFilterEnableStateChangedEventArgs>? SourceFilterEnableStateChanged;
    public event EventHandler<InputMuteStateChangedEventArgs>? InputMuteStateChanged;
    public event EventHandler<InputVolumeChangedEventArgs>? InputVolumeChanged;
    public event EventHandler<VendorEventArgs>? VendorEvent;
    public event EventHandler<MediaInputPlaybackEndedEventArgs>? MediaInputPlaybackEnded;
    public event EventHandler<MediaInputPlaybackStartedEventArgs>? MediaInputPlaybackStarted;
    public event EventHandler<MediaInputActionTriggeredEventArgs>? MediaInputActionTriggered;
    public event EventHandler<VirtualcamStateChangedEventArgs>? VirtualcamStateChanged;
    public event EventHandler<CurrentSceneCollectionChangingEventArgs>? CurrentSceneCollectionChanging;
    public event EventHandler<CurrentProfileChangingEventArgs>? CurrentProfileChanging;
    public event EventHandler<SourceFilterNameChangedEventArgs>? SourceFilterNameChanged;
    public event EventHandler<InputCreatedEventArgs>? InputCreated;
    public event EventHandler<InputRemovedEventArgs>? InputRemoved;
    public event EventHandler<InputNameChangedEventArgs>? InputNameChanged;
    public event EventHandler<InputActiveStateChangedEventArgs>? InputActiveStateChanged;
    public event EventHandler<InputShowStateChangedEventArgs>? InputShowStateChanged;
    public event EventHandler<InputAudioBalanceChangedEventArgs>? InputAudioBalanceChanged;
    public event EventHandler<InputAudioTracksChangedEventArgs>? InputAudioTracksChanged;
    public event EventHandler<InputAudioMonitorTypeChangedEventArgs>? InputAudioMonitorTypeChanged;
    public event EventHandler<InputVolumeMetersEventArgs>? InputVolumeMeters;
    public event EventHandler<ReplayBufferSavedEventArgs>? ReplayBufferSaved;
    public event EventHandler<SceneCreatedEventArgs>? SceneCreated;
    public event EventHandler<SceneRemovedEventArgs>? SceneRemoved;
    public event EventHandler<SceneNameChangedEventArgs>? SceneNameChanged;

    protected void SendIdentify(string password, ObsAuthInfo authInfo = null)
    {
        var requestFields = new JObject
        {
            { "rpcVersion", SUPPORTED_RPC_VERSION }
        };

        if (authInfo != null)
        {
            string secret = HashEncode(password + authInfo.PasswordSalt);
            string authResponse = HashEncode(secret + authInfo.Challenge);
            requestFields.Add("authentication", authResponse);
        }

        SendRequest(MessageTypes.Identify, null, requestFields, false);
    }

    protected string HashEncode(string input)
    {
        using var sha256 = new SHA256Managed();

        byte[] textBytes = Encoding.ASCII.GetBytes(input);
        byte[] hash = sha256.ComputeHash(textBytes);

        return Convert.ToBase64String(hash);
    }

    protected string NewMessageID(int length = 16)
    {
        const string pool = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        string result = "";
        for (int i = 0; i < length; i++)
        {
            int index = random.Next(0, pool.Length - 1);
            result += pool[index];
        }

        return result;
    }

    private void HandleHello(JObject payload)
    {
        if (!wsConnection.IsStarted)
        {
            return;
        }

        ObsAuthInfo authInfo = null;
        if (payload.ContainsKey("authentication"))
        {
            authInfo = new ObsAuthInfo((JObject)payload["authentication"]);
        }

        SendIdentify(connectionPassword, authInfo);

        connectionPassword = null;
    }
    
    private const string REQUEST_FIELD_VOLUME_DB = "inputVolumeDb";
    private const string REQUEST_FIELD_VOLUME_MUL = "inputVolumeMul";
    private const string RESPONSE_FIELD_IMAGE_DATA = "imageData";
    
    public ObsVideoSettings GetVideoSettings()
    {
        JObject response = SendRequest(nameof(GetVideoSettings));
        return JsonConvert.DeserializeObject<ObsVideoSettings>(response.ToString());
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
        return (string)response["imageData"];
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
        return (string)response["currentProgramSceneName"];
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
        return JsonConvert.DeserializeObject<ObsStats>(response.ToString());
    }

    public List<SceneBasicInfo> ListScenes()
    {
        var response = GetSceneList();
        return response.Scenes;
    }

    public GetSceneListInfo GetSceneList()
    {
        JObject response = SendRequest(nameof(GetSceneList));
        return JsonConvert.DeserializeObject<GetSceneListInfo>(response.ToString());
    }

    public TransitionOverrideInfo GetSceneSceneTransitionOverride(string sceneName)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName }
        };

        JObject response = SendRequest(nameof(GetSceneSceneTransitionOverride), request);
        return response.ToObject<TransitionOverrideInfo>();
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

        return JsonConvert.DeserializeObject<List<FilterSettings>>(response["filters"].ToString());
    }

    public FilterSettings GetSourceFilter(string sourceName, string filterName)
    {
        var request = new JObject
        {
            { nameof(sourceName), sourceName },
            { nameof(filterName), filterName }
        };

        JObject response = SendRequest(nameof(GetSourceFilter), request);
        return JsonConvert.DeserializeObject<FilterSettings>(response.ToString());
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
        return (bool)response["outputActive"];
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

        var response = SendRequest(nameof(SetCurrentSceneTransitionSettings), requestFields);
    }

    public void SetInputVolume(string inputName, float inputVolume, bool inputVolumeDb = false)
    {
        var requestFields = new JObject
        {
            { nameof(inputName), inputName }
        };

        if (inputVolumeDb)
        {
            requestFields.Add(REQUEST_FIELD_VOLUME_DB, inputVolume);
        }
        else
        {
            requestFields.Add(REQUEST_FIELD_VOLUME_MUL, inputVolume);
        }

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
        return (bool)response["inputMuted"];
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
        return (string)currentCollectionName;
    }

    public List<string> GetSceneCollectionList()
    {
        var response = SendRequest(nameof(GetSceneCollectionList));
        return JsonConvert.DeserializeObject<List<string>>(response["sceneCollections"].ToString());
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
        return JsonConvert.DeserializeObject<GetProfileListInfo>(response.ToString());
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
        return (string)response["outputPath"];
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
        return (string)response["recordDirectory"];
    }

    RecordingStatus IObsWebsocket.GetRecordStatus()
    {
        return GetRecordStatus();
    }

    public RecordingStatus GetRecordStatus()
    {
        var response = SendRequest(nameof(GetRecordStatus));
        return JsonConvert.DeserializeObject<RecordingStatus>(response.ToString());
    }

    public bool GetReplayBufferStatus()
    {
        var response = SendRequest(nameof(GetReplayBufferStatus));
        return (bool)response["outputActive"];
    }

    GetTransitionListInfo IObsWebsocket.GetSceneTransitionList()
    {
        return GetSceneTransitionList();
    }

    public GetTransitionListInfo GetSceneTransitionList()
    {
        var response = SendRequest(nameof(GetSceneTransitionList));

        return JsonConvert.DeserializeObject<GetTransitionListInfo>(response.ToString());
    }

    public bool GetStudioModeEnabled()
    {
        var response = SendRequest(nameof(GetStudioModeEnabled));
        return (bool)response["studioModeEnabled"];
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
        return (string)response["currentPreviewSceneName"];
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
        return (int)response["inputAudioSyncOffset"];
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

    public void DuplicateSceneItem(string sceneName, int sceneItemId, string destinationSceneName = null)
    {
        var requestFields = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(sceneItemId), sceneItemId }
        };

        if (!String.IsNullOrEmpty(destinationSceneName))
        {
            requestFields.Add(nameof(destinationSceneName), destinationSceneName);
        }

        SendRequest(nameof(DuplicateSceneItem), requestFields);
    }

    public Dictionary<string, string> GetSpecialInputs()
    {
        var response = SendRequest(nameof(GetSpecialInputs));
        var sources = new Dictionary<string, string>();
        foreach (KeyValuePair<string, JToken> kvp in response)
        {
            string key = kvp.Key;
            string value = (string)kvp.Value;
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
        var requestFields = new JObject
        {
            { "streamServiceType", service.Type },
            { "streamServiceSettings", JToken.FromObject(service.Settings) }
        };

        SendRequest(nameof(SetStreamServiceSettings), requestFields);
    }

    /// <summary>
    /// Gets the current stream service settings (stream destination).
    /// </summary>
    /// <returns>Stream service type and settings objects</returns>
    public StreamingService GetStreamServiceSettings()
    {
        var response = SendRequest(nameof(GetStreamServiceSettings));

        return JsonConvert.DeserializeObject<StreamingService>(response.ToString());
    }

    /// <summary>
    /// Gets the audio monitor type of an input.
    /// The available audio monitor types are:
    /// - `OBS_MONITORING_TYPE_NONE`
    /// - `OBS_MONITORING_TYPE_MONITOR_ONLY`
    /// - `OBS_MONITORING_TYPE_MONITOR_AND_OUTPUT`
    /// </summary>
    /// <param name="inputName">Source name</param>
    /// <returns>The monitor type in use</returns>
    public string GetInputAudioMonitorType(string inputName)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName }
        };

        var response = SendRequest(nameof(GetInputAudioMonitorType), request);
        return (string)response["monitorType"];
    }

    /// <summary>
    /// Sets the audio monitor type of an input.
    /// </summary>
    /// <param name="inputName">Name of the input to set the audio monitor type of</param>
    /// <param name="monitorType">Audio monitor type. See `GetInputAudioMonitorType for possible types.</param>
    public void SetInputAudioMonitorType(string inputName, string monitorType)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(monitorType), monitorType }
        };

        SendRequest(nameof(SetInputAudioMonitorType), request);
    }

    /// <summary>
    /// Broadcasts a `CustomEvent` to all WebSocket clients. Receivers are clients which are identified and subscribed.
    /// </summary>
    /// <param name="eventData">Data payload to emit to all receivers</param>
    public void BroadcastCustomEvent(JObject eventData)
    {
        var request = new JObject
        {
            { nameof(eventData), eventData }
        };

        SendRequest(nameof(BroadcastCustomEvent), request);
    }

    /// <summary>
    /// Sets the cursor position of a media input.
    /// This request does not perform bounds checking of the cursor position.
    /// </summary>
    /// <param name="inputName">Name of the media input</param>
    /// <param name="mediaCursor">New cursor position to set (milliseconds).</param>
    public void SetMediaInputCursor(string inputName, int mediaCursor)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(mediaCursor), mediaCursor }
        };

        SendRequest(nameof(SetMediaInputCursor), request);
    }

    /// <summary>
    /// Offsets the current cursor position of a media input by the specified value.
    /// This request does not perform bounds checking of the cursor position.
    /// </summary>
    /// <param name="inputName">Name of the media input</param>
    /// <param name="mediaCursorOffset">Value to offset the current cursor position by (milliseconds +/-)</param>
    public void OffsetMediaInputCursor(string inputName, int mediaCursorOffset)
    {
        var request = new JObject
        {
            { nameof(inputName), inputName },
            { nameof(mediaCursorOffset), mediaCursorOffset }
        };

        SendRequest(nameof(OffsetMediaInputCursor), request);
    }

    /// <summary>
    /// Creates a new input, adding it as a scene item to the specified scene.
    /// </summary>
    /// <param name="sceneName">Name of the scene to add the input to as a scene item</param>
    /// <param name="inputName">Name of the new input to created</param>
    /// <param name="inputKind">The kind of input to be created</param>
    /// <param name="inputSettings">Jobject holding the settings object to initialize the input with</param>
    /// <param name="sceneItemEnabled">Whether to set the created scene item to enabled or disabled</param>
    /// <returns>ID of the SceneItem in the scene.</returns>
    public int CreateInput(string sceneName, string inputName, string inputKind, JObject inputSettings,
        bool? sceneItemEnabled)
    {
        var request = new JObject
        {
            { nameof(sceneName), sceneName },
            { nameof(inputName), inputName },
            { nameof(inputKind), inputKind }
        };

        if (inputSettings != null)
        {
            request.Add(nameof(inputSettings), inputSettings);
        }

        if (sceneItemEnabled.HasValue)
        {
            request.Add(nameof(sceneItemEnabled), sceneItemEnabled.Value);
        }

        var response = SendRequest(nameof(CreateInput), request);
        return (int)response["sceneItemId"];
    }

    /// <summary>
    /// Gets the default settings for an input kind.
    /// </summary>
    /// <param name="inputKind">Input kind to get the default settings for</param>
    /// <returns>Object of default settings for the input kind</returns>
    public JObject GetInputDefaultSettings(string inputKind)
    {
        var request = new JObject
        {
            { nameof(inputKind), inputKind }
        };

        var response = SendRequest(nameof(GetInputDefaultSettings), request);
        return (JObject)response["defaultInputSettings"];
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

        var response = SendRequest(nameof(GetSceneItemList), request);
        return response["sceneItems"].Select(m => new SceneItemDetails((JObject)m)).ToList();
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
        return (int)response["sceneItemId"];
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
        return JsonConvert.DeserializeObject<List<string>>(response["hotkeys"].ToString());
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

    public List<InputBasicInfo> GetInputList(string inputKind = null)
    {
        var request = new JObject
        {
            { nameof(inputKind), inputKind }
        };

        var response = inputKind is null
            ? SendRequest(nameof(GetInputList))
            : SendRequest(nameof(GetInputList), request);

        var returnList = new List<InputBasicInfo>();
        foreach (var input in response["inputs"])
        {
            returnList.Add(new InputBasicInfo(input as JObject));
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

        return JsonConvert.DeserializeObject<List<string>>(response["inputKinds"].ToString());
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
        return (double)response["inputAudioBalance"];
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
        return response["propertyItems"].Value<List<JObject>>();
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
        return (string)response["savedReplayPath"];
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
        return JsonConvert.DeserializeObject<List<JObject>>((string)response["sceneItems"]);
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
        return (int)response["sceneItemId"];
    }

    SceneItemTransformInfo IObsWebsocket.GetSceneItemTransform(string sceneName, int sceneItemId)
    {
        return GetSceneItemTransform(sceneName, sceneItemId);
    }

    public SceneItemTransformInfo GetSceneItemTransform(string sceneName, int sceneItemId)
    {
        var response = GetSceneItemTransformRaw(sceneName, sceneItemId);
        return JsonConvert.DeserializeObject<SceneItemTransformInfo>(response["sceneItemTransform"].ToString());
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
        return (bool)response["sceneItemEnabled"];
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
        return (bool)response["sceneItemLocked"];
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
        return (int)response["sceneItemIndex"];
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
        return (string)response["sceneItemBlendMode"];
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
        return JsonConvert.DeserializeObject<List<string>>(response["groups"].ToString());
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
        return (string)response["imageData"];
    }

    public List<string> GetTransitionKindList()
    {
        var response = SendRequest(nameof(GetTransitionKindList));
        return JsonConvert.DeserializeObject<List<string>>(response["transitionKinds"].ToString());
    }

    public double GetCurrentSceneTransitionCursor()
    {
        var response = SendRequest(nameof(GetCurrentSceneTransitionCursor));
        return (double)response["transitionCursor"];
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

        foreach (var monitor in response["monitors"])
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

    protected void ProcessEventType(string eventType, JObject body)
    {
        body = (JObject)body["eventData"];

        switch (eventType)
        {
            case nameof(CurrentProgramSceneChanged):
                CurrentProgramSceneChanged?.Invoke(this, new ProgramSceneChangedEventArgs((string)body["sceneName"]));
                break;

            case nameof(SceneListChanged):
                SceneListChanged?.Invoke(this,
                    new SceneListChangedEventArgs(
                        JsonConvert.DeserializeObject<List<JObject>>((string)body["scenes"])));
                break;

            case nameof(SceneItemListReindexed):
                SceneItemListReindexed?.Invoke(this,
                    new SceneItemListReindexedEventArgs((string)body["sceneName"],
                        JsonConvert.DeserializeObject<List<JObject>>((string)body["sceneItems"])));
                break;

            case nameof(SceneItemCreated):
                SceneItemCreated?.Invoke(this,
                    new SceneItemCreatedEventArgs((string)body["sceneName"], (string)body["sourceName"],
                        (int)body["sceneItemId"], (int)body["sceneItemIndex"]));
                break;

            case nameof(SceneItemRemoved):
                SceneItemRemoved?.Invoke(this,
                    new SceneItemRemovedEventArgs((string)body["sceneName"], (string)body["sourceName"],
                        (int)body["sceneItemId"]));
                break;

            case nameof(SceneItemEnableStateChanged):
                SceneItemEnableStateChanged?.Invoke(this,
                    new SceneItemEnableStateChangedEventArgs((string)body["sceneName"], (int)body["sceneItemId"],
                        (bool)body["sceneItemEnabled"]));
                break;

            case nameof(SceneItemLockStateChanged):
                SceneItemLockStateChanged?.Invoke(this,
                    new SceneItemLockStateChangedEventArgs((string)body["sceneName"], (int)body["sceneItemId"],
                        (bool)body["sceneItemLocked"]));
                break;

            case nameof(CurrentSceneCollectionChanged):
                CurrentSceneCollectionChanged?.Invoke(this,
                    new CurrentSceneCollectionChangedEventArgs((string)body["sceneCollectionName"]));
                break;

            case nameof(SceneCollectionListChanged):
                SceneCollectionListChanged?.Invoke(this,
                    new SceneCollectionListChangedEventArgs(
                        JsonConvert.DeserializeObject<List<string>>((string)body["sceneCollections"])));
                break;

            case nameof(CurrentSceneTransitionChanged):
                CurrentSceneTransitionChanged?.Invoke(this,
                    new CurrentSceneTransitionChangedEventArgs((string)body["transitionName"]));
                break;

            case nameof(CurrentSceneTransitionDurationChanged):
                CurrentSceneTransitionDurationChanged?.Invoke(this,
                    new CurrentSceneTransitionDurationChangedEventArgs((int)body["transitionDuration"]));
                break;

            case nameof(SceneTransitionStarted):
                SceneTransitionStarted?.Invoke(this,
                    new SceneTransitionStartedEventArgs((string)body["transitionName"]));
                break;

            case nameof(SceneTransitionEnded):
                SceneTransitionEnded?.Invoke(this, new SceneTransitionEndedEventArgs((string)body["transitionName"]));
                break;

            case nameof(SceneTransitionVideoEnded):
                SceneTransitionVideoEnded?.Invoke(this,
                    new SceneTransitionVideoEndedEventArgs((string)body["transitionName"]));
                break;

            case nameof(CurrentProfileChanged):
                CurrentProfileChanged?.Invoke(this, new CurrentProfileChangedEventArgs((string)body["profileName"]));
                break;

            case nameof(ProfileListChanged):
                ProfileListChanged?.Invoke(this,
                    new ProfileListChangedEventArgs(
                        JsonConvert.DeserializeObject<List<string>>((string)body["profiles"])));
                break;

            case nameof(StreamStateChanged):
                StreamStateChanged?.Invoke(this, new StreamStateChangedEventArgs(new OutputStateChanged(body)));
                break;

            case nameof(RecordStateChanged):
                RecordStateChanged?.Invoke(this, new RecordStateChangedEventArgs(new RecordStateChanged(body)));
                break;

            case nameof(CurrentPreviewSceneChanged):
                CurrentPreviewSceneChanged?.Invoke(this,
                    new CurrentPreviewSceneChangedEventArgs((string)body["sceneName"]));
                break;

            case nameof(StudioModeStateChanged):
                StudioModeStateChanged?.Invoke(this,
                    new StudioModeStateChangedEventArgs((bool)body["studioModeEnabled"]));
                break;

            case nameof(ReplayBufferStateChanged):
                ReplayBufferStateChanged?.Invoke(this,
                    new ReplayBufferStateChangedEventArgs(new OutputStateChanged(body)));
                break;

            case nameof(ExitStarted):
                ExitStarted?.Invoke(this, EventArgs.Empty);
                break;

            case nameof(SceneItemSelected):
                SceneItemSelected?.Invoke(this,
                    new SceneItemSelectedEventArgs((string)body["sceneName"], (string)body["sceneItemId"]));
                break;

            case nameof(SceneItemTransformChanged):
                SceneItemTransformChanged?.Invoke(this,
                    new SceneItemTransformEventArgs((string)body["sceneName"], (string)body["sceneItemId"],
                        new SceneItemTransformInfo((JObject)body["sceneItemTransform"])));
                break;

            case nameof(InputAudioSyncOffsetChanged):
                InputAudioSyncOffsetChanged?.Invoke(this,
                    new InputAudioSyncOffsetChangedEventArgs((string)body["inputName"],
                        (int)body["inputAudioSyncOffset"]));
                break;

            case nameof(InputMuteStateChanged):
                InputMuteStateChanged?.Invoke(this,
                    new InputMuteStateChangedEventArgs((string)body["inputName"], (bool)body["inputMuted"]));
                break;

            case nameof(InputVolumeChanged):
                InputVolumeChanged?.Invoke(this, new InputVolumeChangedEventArgs(new InputVolume(body)));
                break;

            case nameof(SourceFilterCreated):
                SourceFilterCreated?.Invoke(this,
                    new SourceFilterCreatedEventArgs((string)body["sourceName"], (string)body["filterName"],
                        (string)body["filterKind"], (int)body["filterIndex"], (JObject)body["filterSettings"],
                        (JObject)body["defaultFilterSettings"]));
                break;

            case nameof(SourceFilterRemoved):
                SourceFilterRemoved?.Invoke(this,
                    new SourceFilterRemovedEventArgs((string)body["sourceName"], (string)body["filterName"]));
                break;

            case nameof(SourceFilterListReindexed):
                if (SourceFilterListReindexed != null)
                {
                    List<FilterReorderItem> filters = new List<FilterReorderItem>();
                    JsonConvert.PopulateObject(body["filters"].ToString(), filters);

                    SourceFilterListReindexed?.Invoke(this,
                        new SourceFilterListReindexedEventArgs((string)body["sourceName"], filters));
                }

                break;

            case nameof(SourceFilterEnableStateChanged):
                SourceFilterEnableStateChanged?.Invoke(this,
                    new SourceFilterEnableStateChangedEventArgs((string)body["sourceName"], (string)body["filterName"],
                        (bool)body["filterEnabled"]));
                break;

            case nameof(VendorEvent):
                VendorEvent?.Invoke(this,
                    new VendorEventArgs((string)body["vendorName"], (string)body["eventType"], body));
                break;

            case nameof(MediaInputPlaybackEnded):
                MediaInputPlaybackEnded?.Invoke(this, new MediaInputPlaybackEndedEventArgs((string)body["inputName"]));
                break;

            case nameof(MediaInputPlaybackStarted):
                MediaInputPlaybackStarted?.Invoke(this,
                    new MediaInputPlaybackStartedEventArgs((string)body["sourceName"]));
                break;

            case nameof(MediaInputActionTriggered):
                MediaInputActionTriggered?.Invoke(this,
                    new MediaInputActionTriggeredEventArgs((string)body["inputName"], (string)body["mediaAction"]));
                break;

            case nameof(VirtualcamStateChanged):
                VirtualcamStateChanged?.Invoke(this, new VirtualcamStateChangedEventArgs(new OutputStateChanged(body)));
                break;

            case nameof(CurrentSceneCollectionChanging):
                CurrentSceneCollectionChanging?.Invoke(this,
                    new CurrentSceneCollectionChangingEventArgs((string)body["sceneCollectionName"]));
                break;

            case nameof(CurrentProfileChanging):
                CurrentProfileChanging?.Invoke(this, new CurrentProfileChangingEventArgs((string)body["profileName"]));
                break;

            case nameof(SourceFilterNameChanged):
                SourceFilterNameChanged?.Invoke(this,
                    new SourceFilterNameChangedEventArgs((string)body["sourceName"], (string)body["oldFilterName"],
                        (string)body["filterName"]));
                break;

            case nameof(InputCreated):
                InputCreated?.Invoke(this,
                    new InputCreatedEventArgs((string)body["inputName"], (string)body["inputKind"],
                        (string)body["unversionedInputKind"], (JObject)body["inputSettings"],
                        (JObject)body["defaultInputSettings"]));
                break;

            case nameof(InputRemoved):
                InputRemoved?.Invoke(this, new InputRemovedEventArgs((string)body["inputName"]));
                break;

            case nameof(InputNameChanged):
                InputNameChanged?.Invoke(this,
                    new InputNameChangedEventArgs((string)body["oldInputName"], (string)body["inputName"]));
                break;

            case nameof(InputActiveStateChanged):
                InputActiveStateChanged?.Invoke(this,
                    new InputActiveStateChangedEventArgs((string)body["inputName"], (bool)body["videoActive"]));
                break;

            case nameof(InputShowStateChanged):
                InputShowStateChanged?.Invoke(this,
                    new InputShowStateChangedEventArgs((string)body["inputName"], (bool)body["videoShowing"]));
                break;

            case nameof(InputAudioBalanceChanged):
                InputAudioBalanceChanged?.Invoke(this,
                    new InputAudioBalanceChangedEventArgs((string)body["inputName"],
                        (double)body["inputAudioBalance"]));
                break;

            case nameof(InputAudioTracksChanged):
                InputAudioTracksChanged?.Invoke(this,
                    new InputAudioTracksChangedEventArgs((string)body["inputName"], (JObject)body["inputAudioTracks"]));
                break;

            case nameof(InputAudioMonitorTypeChanged):
                InputAudioMonitorTypeChanged?.Invoke(this,
                    new InputAudioMonitorTypeChangedEventArgs((string)body["inputName"], (string)body["monitorType"]));
                break;

            case nameof(InputVolumeMeters):
                InputVolumeMeters?.Invoke(this,
                    new InputVolumeMetersEventArgs(
                        JsonConvert.DeserializeObject<List<JObject>>((string)body["inputs"])));
                break;

            case nameof(ReplayBufferSaved):
                ReplayBufferSaved?.Invoke(this, new ReplayBufferSavedEventArgs((string)body["savedReplayPath"]));
                break;

            case nameof(SceneCreated):
                SceneCreated?.Invoke(this, new SceneCreatedEventArgs((string)body["sceneName"], (bool)body["isGroup"]));
                break;

            case nameof(SceneRemoved):
                SceneRemoved?.Invoke(this, new SceneRemovedEventArgs((string)body["sceneName"], (bool)body["isGroup"]));
                break;

            case nameof(SceneNameChanged):
                SceneNameChanged?.Invoke(this,
                    new SceneNameChangedEventArgs((string)body["oldSceneName"], (string)body["sceneName"]));
                break;

            default:
                var message = $"Unsupported Event: {eventType}\n{body}";
                Console.WriteLine(message);
                Debug.WriteLine(message);
                break;
        }
    }
}