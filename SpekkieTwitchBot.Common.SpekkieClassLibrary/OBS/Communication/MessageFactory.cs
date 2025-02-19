using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.OBS.Enum;

namespace SpekkieClassLibrary.OBS.Communication;

public static class MessageFactory
{
    public static JObject BuildMessage(MessageTypes opCode, string messageType, JObject? additionalFields,
        out string messageId)
    {
        messageId = Guid.NewGuid().ToString();
        var payload = new JObject
        {
            { "op", (int)opCode }
        };

        var data = new JObject();

        switch (opCode)
        {
            case MessageTypes.Request:
                data.Add("requestType", messageType);
                data.Add("requestId", messageId);
                data.Add("requestData", additionalFields);
                additionalFields = new JObject();
                break;
            case MessageTypes.RequestBatch:
                data.Add("requestId", messageId);
                break;
        }

        if (additionalFields != null) data.Merge(additionalFields);
        payload.Add("d", data);
        return payload;
    }
}