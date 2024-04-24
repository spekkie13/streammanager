namespace SpekkieClassLibrary.Twitch.Auth;

public class AuthorizationCredentials
{
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public string? RefreshToken { get; set; }
    public List<string>? Scope { get; set; }
    public string? TokenType { get; set; }
}
