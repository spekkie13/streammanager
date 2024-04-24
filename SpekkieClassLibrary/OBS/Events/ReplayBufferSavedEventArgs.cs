namespace SpekkieClassLibrary.OBS.Events;

public class ReplayBufferSavedEventArgs : EventArgs
{
    public string SavedReplayPath { get; }

    public ReplayBufferSavedEventArgs(string savedReplayPath)
    {
        SavedReplayPath = savedReplayPath;
    }
}