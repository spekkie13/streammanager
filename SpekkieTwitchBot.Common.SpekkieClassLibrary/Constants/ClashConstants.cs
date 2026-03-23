namespace SpekkieClassLibrary.Constants;

public class ClashConstants
{
    public static readonly string BaseDir =
        Environment.GetEnvironmentVariable("BOT_BASE_DIR")
        ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    public static readonly string OutputDir = $"{BaseDir}/Output/ClashOfClans";
    public static readonly string HomeFolder = $"{OutputDir}{Path.DirectorySeparatorChar}home";
    public static readonly string AwayFolder = $"{OutputDir}{Path.DirectorySeparatorChar}away";
    public const string ClanApiBaseUrl = "https://api.clashofclans.com/v1/clans/";
    public const string PlayerApiBaseUrl = "https://api.clashofclans.com/v1/players/";
    public const string DebugClanTag = "#29Y29P98J";

    public const string DebugApiToken =
        "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiIsImtpZCI6IjI4YTMxOGY3LTAwMDAtYTFlYi03ZmExLTJjNzQzM2M2Y2NhNSJ9.eyJpc3MiOiJzdXBlcmNlbGwiLCJhdWQiOiJzdXBlcmNlbGw6Z2FtZWFwaSIsImp0aSI6IjYyYmJhOTZlLWIyZWQtNDEzMy05NjM0LTAzNmI3MzI3MTZjOSIsImlhdCI6MTc3MzY4MDg5OSwic3ViIjoiZGV2ZWxvcGVyLzM1NDRiZTgyLWYxZTQtMjAxYS1lNWIyLTRlMzc1Yjk1YTc3OSIsInNjb3BlcyI6WyJjbGFzaCJdLCJsaW1pdHMiOlt7InRpZXIiOiJkZXZlbG9wZXIvc2lsdmVyIiwidHlwZSI6InRocm90dGxpbmcifSx7ImNpZHJzIjpbIjgzLjg1LjIxNy4xNzYiXSwidHlwZSI6ImNsaWVudCJ9XX0.tvgaKyYYTC4B6dOmWD7EfvbXid2FTRspJKPon5u7FVOzAE2cXQuXaEcgxczKnzGDiYomaQkmRtOJasvvDG0_rw";

    public static readonly Dictionary<string, string> WarStatus = new()
    {
        { "CLAN_NOT_FOUND", "The requested clan was not found, check the clan tag" },
        { "ACCESS_DENIED", "Access denied, check your api token" },
        { "NOT_IN_WAR", "Not in war at this moment" },
        { "IN_MATCHMAKING", "In matchmaking" },
        { "ENTER_WAR", "Entering war" },
        { "MATCHED", "Just matched" },
        { "PREPARATION", "In preparation day" },
        { "WAR", "In war?" },
        { "INWAR", "In war day" }
    };
}