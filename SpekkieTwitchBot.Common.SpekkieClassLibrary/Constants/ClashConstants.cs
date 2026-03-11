namespace SpekkieClassLibrary.Constants;

public class ClashConstants
{
    public static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    public static string OutputDir = $"{BaseDir}/Output/ClashOfClans";
    public static string HomeFolder = $"{OutputDir}{Path.DirectorySeparatorChar}home";
    public static string AwayFolder = $"{OutputDir}{Path.DirectorySeparatorChar}away";
    public const string ClanApiBaseUrl = "https://api.clashofclans.com/v1/clans/";
    public const string PlayerApiBaseUrl = "https://api.clashofclans.com/v1/players/";
    public const string DebugClanTag = "#29Y29P98J";

    public const string DebugApiToken =
        "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzUxMiIsImtpZCI6IjI4YTMxOGY3LTAwMDAtYTFlYi03ZmExLTJjNzQzM2M2Y2NhNSJ9.eyJpc3MiOiJzdXBlcmNlbGwiLCJhdWQiOiJzdXBlcmNlbGw6Z2FtZWFwaSIsImp0aSI6ImNhNzI4Y2M3LTFlMzQtNDFmOC1hODRkLWM3Y2Y2MjkyNWRiYiIsImlhdCI6MTY3NzI1MDI1NCwic3ViIjoiZGV2ZWxvcGVyLzM1NDRiZTgyLWYxZTQtMjAxYS1lNWIyLTRlMzc1Yjk1YTc3OSIsInNjb3BlcyI6WyJjbGFzaCJdLCJsaW1pdHMiOlt7InRpZXIiOiJkZXZlbG9wZXIvc2lsdmVyIiwidHlwZSI6InRocm90dGxpbmcifSx7ImNpZHJzIjpbIjgzLjgzLjQ2LjE1MiJdLCJ0eXBlIjoiY2xpZW50In1dfQ.7JtPjp0AttT9vwUWGd_PpbMyKHrISvstNOHd9HW1-L2fz30LgdBA2kWbBVXRx6Bl6RHZ-Mg9iejCsMzH2gknXQ";

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