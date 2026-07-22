namespace SpotifyAuthService.Auth;

// Thrown when Spotify refuses to mint an access token (revoked refresh token, bad client secret, ...).
// Distinct from a transient HTTP failure so callers can back off instead of retrying every tick.
public class SpotifyAuthException : Exception
{
    public SpotifyAuthException(string message) : base(message)
    {
    }
}
