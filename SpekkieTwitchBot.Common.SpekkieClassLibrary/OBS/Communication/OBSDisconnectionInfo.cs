using SpekkieClassLibrary.OBS.Enum;
using Websocket.Client;

namespace SpekkieClassLibrary.OBS.Communication;

public class ObsDisconnectionInfo(
    ObsCloseCodes obsCloseCode,
    string disconnectReason,
    DisconnectionInfo websocketDisconnectionInfo)
{
    public ObsCloseCodes ObsCloseCode { get; private set; } = obsCloseCode;
    public string DisconnectReason { get; set; } = disconnectReason;
    public DisconnectionInfo WebsocketDisconnectionInfo { get; private set; } = websocketDisconnectionInfo;
}