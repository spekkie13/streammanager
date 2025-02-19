#nullable disable
namespace SpekkieClassLibrary.Spotify.Auth;

public class AuthorizationCodeTokenResponse
{
    public string RefreshToken { get; set; }
    public string TokenType { get; set; }
    public int ExpiresIn { get; init; }
    public string Scope { get; set; }
    public string AccessToken { get; init; }
    private DateTime CreatedAt { get; } = DateTime.UtcNow;

    public bool IsExpired => CreatedAt.AddSeconds(ExpiresIn) <= DateTime.UtcNow;
}