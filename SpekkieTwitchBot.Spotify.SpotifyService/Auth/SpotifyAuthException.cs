namespace SpotifyAuthService.Auth;

// Thrown when Spotify refuses to mint an access token (revoked refresh token, bad client secret, ...).
// Distinct from a transient HTTP failure so callers can back off instead of retrying every tick.
public class SpotifyAuthException : Exception
{
    // True when Spotify rejected the grant itself (invalid_grant): the refresh token is dead and
    // no amount of retrying will revive it. Only a human re-running the authorization-code flow
    // can fix it, so callers should stop polling rather than back off.
    public bool RequiresReauthorization { get; }

    public SpotifyAuthException(string message, bool requiresReauthorization = false) : base(message)
    {
        RequiresReauthorization = requiresReauthorization;
    }
}
