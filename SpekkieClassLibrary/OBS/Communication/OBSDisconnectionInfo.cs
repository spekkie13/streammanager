using SpekkieClassLibrary.OBS.Enum;
using Websocket.Client;

namespace SpekkieClassLibrary.OBS.Communication;

public class ObsDisconnectionInfo
{
    public ObsCloseCodes ObsCloseCode { get; private set; }
    public string DisconnectReason { get; set; }
    public DisconnectionInfo WebsocketDisconnectionInfo { get; private set; }
    public ObsDisconnectionInfo(
        ObsCloseCodes obsCloseCode, 
        string disconnectReason, 
        DisconnectionInfo websocketDisconnectionInfo)
    {
        ObsCloseCode = obsCloseCode;
        DisconnectReason = disconnectReason;
        WebsocketDisconnectionInfo = websocketDisconnectionInfo;
    }
}