using System.Net;

namespace SpekkieTwitchBot.Models.Spotify.Interface;

public interface IResponse
{
    object? Body { get; }

    IReadOnlyDictionary<string, string> Headers { get; }

    HttpStatusCode StatusCode { get; }

    string? ContentType { get; }
}