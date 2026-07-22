namespace SpekkieTwitchBot.Systems.Overlay;

/// <summary>
/// Lets the !afk / !back commands flip the overlay's afk and timerRunning flags together and immediately
/// persist overlay-state.json through the existing writer (<see cref="OverlayStateService"/>) — the bot
/// stays the only writer of overlay state. Going AFK pauses the timer, so both flags move in one write.
/// </summary>
public interface IOverlayAfkWriter
{
    Task SetAfkAsync(bool afk, bool timerRunning, CancellationToken ct);
}
