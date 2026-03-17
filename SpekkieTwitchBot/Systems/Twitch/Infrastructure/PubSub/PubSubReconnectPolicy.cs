namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

public sealed class PubSubReconnectPolicy
{
    private int _Attempt;
    private readonly Random _Rng = new();

    public TimeSpan NextDelay()
    {
        _Attempt = Math.Min(_Attempt + 1, 8); // cap
        double baseMs = Math.Pow(2, _Attempt) * 250; // 250ms, 500, 1s, 2s, 4s...
        int jitter = _Rng.Next(0, 250);
        return TimeSpan.FromMilliseconds(Math.Min(baseMs + jitter, 30_000));
    }

    public void Reset() => _Attempt = 0;
}