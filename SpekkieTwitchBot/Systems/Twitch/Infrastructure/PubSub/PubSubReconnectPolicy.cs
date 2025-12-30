namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public sealed class PubSubReconnectPolicy
{
    private int _attempt = 0;
    private readonly Random _rng = new();

    public TimeSpan NextDelay()
    {
        _attempt = Math.Min(_attempt + 1, 8); // cap
        var baseMs = Math.Pow(2, _attempt) * 250; // 250ms, 500, 1s, 2s, 4s...
        var jitter = _rng.Next(0, 250);
        return TimeSpan.FromMilliseconds(Math.Min(baseMs + jitter, 30_000));
    }

    public void Reset() => _attempt = 0;
}