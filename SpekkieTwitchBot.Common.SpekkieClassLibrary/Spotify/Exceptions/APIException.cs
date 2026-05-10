using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpekkieClassLibrary.Spotify.Interface;
using SpekkieClassLibrary.Spotify.Internal;

namespace SpekkieClassLibrary.Spotify.Exceptions;

[Serializable]
public class ApiException : Exception
{
    public ApiException(IResponse response) : base(ParseApiErrorMessage(response))
    {
        Ensure.ArgumentNotNull(response, nameof(response));

        Response = response;
    }

    public ApiException()
    {
    }

    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    [Obsolete("Obsolete")]
    protected ApiException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        Response = info.GetValue("APIException.Response", typeof(IResponse)) as IResponse;
    }

    public IResponse? Response { get; set; }

    private static string ParseApiErrorMessage(IResponse response)
    {
        string? body = response.Body as string;
        if (string.IsNullOrEmpty(body)) return "";

        try
        {
            JObject bodyObject = JObject.Parse(body);

            JToken? error = bodyObject.Value<JToken>("error");
            if (error == null) return "";

            switch (error.Type)
            {
                case JTokenType.String:
                    return error.ToString();
                case JTokenType.Object:
                    return error.Value<string>("message") ?? "";
            }
        }
        catch (JsonReaderException)
        {
            return "";
        }

        return "";
    }

    [Obsolete(
        "This API supports obsolete formatter-based serialization. It should not be called or extended by application code.",
        DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue("APIException.Response", Response);
    }
}