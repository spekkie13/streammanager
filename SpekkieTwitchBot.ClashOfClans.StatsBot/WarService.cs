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
    private readonly Dictionary<string, byte[]> _logoCache = new();
    private string? _lastWarState;

    public bool IsWarActive => _lastWarState is "preparation" or "inWar";
    private CancellationTokenSource? _watcherDebounce;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartFileWatchers(stoppingToken);
        await FetchWar();

        while (!stoppingToken.IsCancellationRequested)
        {
            await FetchWar();
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private void StartFileWatchers(CancellationToken stoppingToken)
    {
        string dir = ClashConstants.OutputDir;
        WatchFile(dir, "clan tag.txt", stoppingToken);
        WatchFile(dir, "player tag.txt", stoppingToken);
    }

    private void WatchFile(string dir, string file, CancellationToken stoppingToken)
    {
        FileSystemWatcher watcher = new(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        watcher.Changed += (_, _) => OnTagFileChanged(stoppingToken);
        stoppingToken.Register(watcher.Dispose);
    }

    private void OnTagFileChanged(CancellationToken stoppingToken)
    {
        _watcherDebounce?.Cancel();
        _watcherDebounce?.Dispose();
        _watcherDebounce = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        CancellationTokenSource cts = _watcherDebounce;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, cts.Token);
                logger.LogWarning("[CoC] Tag file changed — triggering immediate war fetch");
                await FetchWar();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError($"[CoC] Error on tag file change: {ex.Message}");
            }
        }, cts.Token);
    }

    public void SetWarMode(WarDisplayMode mode)
    {
        warStatus.Mode = mode;
        Console.WriteLine($"War display mode: {mode}");
    }

    private async Task FetchWar()
    {
        try
        {
            string clanTagPath = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clan tag.txt";
            string clanTag = (await reader.ReadAsync(clanTagPath)).Replace("\r", "").Replace("\n", "").Trim();

            if (string.IsNullOrEmpty(clanTag))
            {
                string playerTagPath = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}player tag.txt";
                string playerTag = (await reader.ReadAsync(playerTagPath)).Replace("\r", "").Replace("\n", "").Trim();

                if (string.IsNullOrEmpty(playerTag))
                {
                    logger.LogWarning("[CoC] Both clan tag and player tag are empty — skipping war fetch");
                    return;
                }

                logger.LogWarning("[CoC] Clan tag is empty — looking up clan from player tag");
                clanTag = await client.GetPlayerClan(playerTag);

                if (string.IsNullOrEmpty(clanTag))
                {
                    logger.LogWarning("[CoC] Could not resolve clan tag from player tag — player may not be in a clan");
                    return;
                }

                await writer.WriteAsync(clanTagPath, clanTag);
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
            if (_lastWarState != "notInWar")
            {
                _lastWarState = "notInWar";
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

        if (runTimeWar.State != _lastWarState)
        {
            _lastWarState = runTimeWar.State;
            await eventBus.PublishAsync(new WarStateChangedEvent(runTimeWar.State, runTimeWar.Clan.Name, runTimeWar.Opponent.Name));
        }

        if (runTimeWar.State == "preparation" && manager.IsNewWar(runTimeWar.PreparationStartTime))
        {
            Console.WriteLine("New war detected, resetting war files...");
            _logoCache.Clear();
            manager.ResetWarFiles();
            manager.SaveWarId(runTimeWar.PreparationStartTime);
        }

        await ProcessTeam(clan: runTimeWar.Clan, team: "home", teamFolder: ClashConstants.HomeFolder);
        await ProcessTeam(clan: runTimeWar.Opponent, team: "away", teamFolder: ClashConstants.AwayFolder);
    }

    private async Task<byte[]> GetTeamLogoAsync(RunTimeClan clan)
    {
        if (_logoCache.TryGetValue(clan.Tag, out byte[]? cached))
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

        _logoCache[clan.Tag] = logo;
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

    public WarDisplayMode GetWarMode() => warStatus.Mode;

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
