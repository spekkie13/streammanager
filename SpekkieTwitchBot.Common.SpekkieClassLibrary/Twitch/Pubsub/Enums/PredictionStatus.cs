namespace SpekkieClassLibrary.Twitch.Pubsub.Enums;

public enum PredictionStatus
{
    Canceled = -4, // 0xFFFFFFFC
    CancelPending = -3, // 0xFFFFFFFD
    Resolved = -2, // 0xFFFFFFFE
    ResolvePending = -1, // 0xFFFFFFFF
    Locked = 0,
    Active = 1
}