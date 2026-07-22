namespace SpekkieClassLibrary.Overlay;

// The active sub goal surfaced to the overlay: the current count, the next milestone's target, and the
// reward unlocked at that target. Goal/Reward reflect the next unreached tier (empty once all reached).
public class OverlaySubGoal
{
    public int Current { get; init; }
    public int Goal { get; init; }
    public string Reward { get; init; } = "";
}
