namespace SpekkieTwitchBot.FileHandling.Twitch;

public class TwitchFileSetup
{
    private readonly FileSetup _fileSetup;
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";

    public TwitchFileSetup(FileSetup fileSetup)
    {
        _fileSetup = fileSetup;
    }
}