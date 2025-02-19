namespace SpekkieClassLibrary.OBS.Events;

public class ReplayBufferSavedEventArgs : EventArgs
{
    public ReplayBufferSavedEventArgs(string savedReplayPath)
    {
        SavedReplayPath = savedReplayPath;
    }

    public string SavedReplayPath { get; }
}