using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.ClashOfClans.Ccn;
using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Constants;
using SpekkieClassLibrary.Events;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Clash;

namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public class WarService(
    ClashFileReader reader,
    ClashFileWriter writer,
    ClashFileManager manager,
    CocHttpClient client,
    CcnHttpClient ccnClient,
    WarStatus warStatus,
    IStreamEventBus eventBus,
    Logger logger)
    : BackgroundService, IWarService
{
    private readonly Dictionary<string, byte[]> _LogoCache = new();
    private string? _LastWarState;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await FetchWar();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (warStatus.GetStatus())
                await FetchWar();
            else
                logger.LogInfo("War stats inactive");

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    public void SetWarStats(bool enable)
    {
        warStatus.SetStatus(enable);
        Console.WriteLine($"War stats active: {enable}");
    }

    private async Task FetchWar()
    {
        try
        {
            string clanTagPath = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clan tag.txt";
            string clanTag = await reader.ReadAsync(clanTagPath);
            clanTag = clanTag.Replace("\r", "").Replace("\n", "");
            if (string.IsNullOrEmpty(clanTag))
            {
                logger.LogWarning("Clan tag is empty");
                return;
            }

            RunTimeWar? runTimeWar = await client.FetchWar(clanTag);
            if (runTimeWar != null)
                await ProcessWar(runTimeWar);
        }
        catch (Exception e)
        {
            logger.LogError(e.StackTrace ?? e.Message);
        }
    }

    private async Task ProcessWar(RunTimeWar runTimeWar)
    {
        if (runTimeWar.Clan == null || runTimeWar.Opponent == null)
        {
            logger.LogWarning("No war detected...");
            if (_LastWarState != "notInWar")
            {
                _LastWarState = "notInWar";
                await eventBus.PublishAsync(new WarStateChangedEvent("notInWar", null, null));
            }
            return;
        }

        switch (runTimeWar.State)
        {
            case "preparation":
            case "inWar":
                logger.LogInfo($"Tracking war: {runTimeWar.Clan.Name} vs {runTimeWar.Opponent.Name}, Status: {ClashConstants.WarStatus[runTimeWar.State.ToUpper()]}");
                break;
            default:
                logger.LogInfo($"{runTimeWar.Clan.Name} is currently not in war");
                break;
        }
        logger.LogInfo("Updated: " + DateTime.Now);

        if (runTimeWar.State != _LastWarState)
        {
            _LastWarState = runTimeWar.State;
            await eventBus.PublishAsync(new WarStateChangedEvent(runTimeWar.State, runTimeWar.Clan.Name, runTimeWar.Opponent.Name));
        }

        if (runTimeWar.State == "preparation" && manager.IsNewWar(runTimeWar.PreparationStartTime))
        {
            Console.WriteLine("New war detected, resetting war files...");
            _LogoCache.Clear();
            manager.ResetWarFiles();
            manager.SaveWarId(runTimeWar.PreparationStartTime);
        }

        await ProcessTeam(clan: runTimeWar.Clan, team: "home", teamFolder: ClashConstants.HomeFolder);
        await ProcessTeam(clan: runTimeWar.Opponent, team: "away", teamFolder: ClashConstants.AwayFolder);
    }

    private async Task<byte[]> GetTeamLogoAsync(RunTimeClan clan)
    {
        if (_LogoCache.TryGetValue(clan.Tag, out byte[]? cached))
            return cached;

        CcnClanInfo? ccnInfo = await ccnClient.GetClanInfoAsync(clan.Tag);
        byte[] logo;
        if (!string.IsNullOrEmpty(ccnInfo?.LogoUrl))
        {
            logger.LogInfo($"[CCN] Using CCN logo for '{clan.Name}'");
            logo = await client.GetByteArrayAsync(ccnInfo.LogoUrl);
        }
        else
        {
            logger.LogInfo($"[CCN] '{clan.Name}' not found on CCN, using CoC badge");
            logo = await client.GetByteArrayAsync(clan.BadgeUrls.Large);
        }

        _LogoCache[clan.Tag] = logo;
        return logo;
    }

    private async Task ProcessTeam(RunTimeClan clan, string team, string teamFolder)
    {
        byte[] logo = await GetTeamLogoAsync(clan);
        manager.CreateTeamLogoFile(logo, team);
        await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} name.txt", clan.Name);
        await manager.WritePlayerNames(clan, teamFolder);

        await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} score.txt", clan.Stars.ToString());
        string percentage = Math.Round(clan.DestructionPercentage, 2).ToString("F2");
        await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} percentage.txt", percentage + "%");

        foreach (RunTimeMember member in clan.Members)
        {
            string pictureName = teamFolder + $"{Path.DirectorySeparatorChar}hit " + member.MapPosition + ".png";
            if (member.Attacks == null)
            {
                CocHttpClient.LoadImage($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}BaseImages{Path.DirectorySeparatorChar}Unhit_Base.png", pictureName);
                await writer.WriteAsync($"{teamFolder}{Path.DirectorySeparatorChar}hit {member.MapPosition}.txt", "0%");
                await writer.WriteAsync(teamFolder + $"{Path.DirectorySeparatorChar}hit " + member.MapPosition + " time.txt", "");
                continue;
            }
            string picToCopy = member.Attacks.First().Stars switch
            {
                0 => $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}BaseImages{Path.DirectorySeparatorChar}0_star.png",
                1 => $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}BaseImages{Path.DirectorySeparatorChar}1_star.png",
                2 => $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}BaseImages{Path.DirectorySeparatorChar}2_star.png",
                3 => $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}BaseImages{Path.DirectorySeparatorChar}3_star.png",
                _ => $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}BaseImages{Path.DirectorySeparatorChar}Unhit_Base.png"
            };

            CocHttpClient.LoadImage(picToCopy, pictureName);
            string fileName = $"{teamFolder}{Path.DirectorySeparatorChar}hit {member.MapPosition}.txt";
            await writer.WriteAsync(fileName, member.Attacks.First().DestructionPercentage + "%");

            int minutes = (int)member.Attacks.First().Duration / 60;
            int seconds = (int)member.Attacks.First().Duration % 60;

            fileName = teamFolder + $"{Path.DirectorySeparatorChar}hit " + member.MapPosition + " time.txt";
            await writer.WriteAsync(fileName, $"{minutes}:{seconds.ToString().PadLeft(2, '0')}");
        }

        List<double> times = (from member in clan.Members
            from attack in member.Attacks ?? Enumerable.Empty<RunTimeAttack>()
            select attack.Duration).ToList();

        double avgTime = 0;
        if (times.Count > 0)
            avgTime = times.Average();

        int sec = (int)avgTime % 60;
        int min = (int)avgTime / 60;

        string averageTime = $"{min}:{sec.ToString().PadLeft(2, '0')}";
        await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} avg time.txt", averageTime);
    }

    public bool GetWarStatus()
    {
        return warStatus.GetStatus();
    }

    public async Task UpdatePlayerTag(string playerTag)
    {
        try
        {
            string playerFile = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}player tag.txt";
            await writer.WriteAsync(playerFile, playerTag);
            string clanTag = await client.GetPlayerClan(playerTag);
            await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clan tag.txt", clanTag);
        }
        catch (Exception e)
        {
            logger.LogError(e.StackTrace ?? e.Message);
        }
    }
}
