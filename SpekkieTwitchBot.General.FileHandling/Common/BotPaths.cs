namespace SpekkieTwitchBot.General.FileHandling.Common;

public static class BotPaths
{
    public static readonly string BaseDir =
        Environment.GetEnvironmentVariable("BOT_BASE_DIR")
        ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
}
