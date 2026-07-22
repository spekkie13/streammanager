namespace SpekkieTwitchBot.Systems.Overlay;

/// <summary>
/// Lets the !pausetimer / !starttimer commands flip the overlay's timerRunning flag and immediately
/// persist overlay-state.json through the existing writer (<see cref="OverlayStateService"/>) — the bot
/// stays the only writer of overlay state.
/// </summary>
public interface IOverlayTimerWriter
{
    Task SetTimerRunningAsync(bool running, CancellationToken ct);
}
