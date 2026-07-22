namespace SpekkieClassLibrary.Overlay;

public class OverlayState
{
    public string Mode { get; init; } = "war";
    public string Layout { get; init; } = "clashLandscape";
    public string UpdatedAt { get; init; } = "";
    // Marathon timer remaining-time text (from Output/Timer/timer.txt), e.g. "07:56:00".
    public string Timer { get; init; } = "";
    public bool IsWarActive { get; init; }
    // True while the marathon countdown is running. Flipped by !pausetimer (false) / !starttimer (true)
    // so the overlay can show a paused treatment without waiting on the timer text to stop changing.
    public bool TimerRunning { get; init; }
    // True while the broadcaster is on a break (!afk). The overlay shows a "be right back" treatment;
    // the timer is paused at the same time, but only this flag is surfaced in the state.
    public bool Afk { get; init; }
    public string AccountName { get; init; } = "";
    public OverlayEventInfo Event { get; init; } = new();
    public OverlayWar War { get; init; } = new();
    public OverlaySupport Support { get; init; } = new();
    public OverlayInfo Info { get; init; } = new();
}
