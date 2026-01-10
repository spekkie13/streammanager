namespace SpekkieTwitchBot.General.FileHandling.General;

public static class Probe
{
    private static int _I;

    public static void Log(string msg)
    {
        var n = Interlocked.Increment(ref _I);
        Console.WriteLine($"[PROBE {n:0000}] [t{Environment.CurrentManagedThreadId}] {msg}");
    }
}