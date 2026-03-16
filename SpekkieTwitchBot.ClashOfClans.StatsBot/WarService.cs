using System.Globalization;
using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Constants;
using SpekkieTwitchBot.General.FileHandling.Clash;

namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public class WarService(
    ClashFileReader reader,
    ClashFileWriter writer,
    ClashFileManager manager,
    CocHttpClient client,
    WarStatus warStatus)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (warStatus.GetStatus())
                FetchWar();
            else
                Console.WriteLine("War stats inactive");

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    public async void ToggleWarStats()
    {
        try
        {
            bool status = warStatus.GetStatus();
            warStatus.SetStatus(!status);
            Console.WriteLine($"War stats active: {!status}");
            // string playerFile = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}player tag.txt";
            // string playerTag = await reader.ReadAsync(playerFile);
            // string clanTag = await client.GetPlayerClan(playerTag);
            // await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clan tag.txt", clanTag);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
    }

    private async void FetchWar()
    {
        try
        {
            string clanTagPath = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clan tag.txt";
            string clanTag = await reader.ReadAsync(clanTagPath);
            clanTag = clanTag.Replace("\r", "").Replace("\n", "");
            if (string.IsNullOrEmpty(clanTag))
            {
                Console.WriteLine("Clan tag is empty");
                return;
            }

            RunTimeWar? runTimeWar = await client.FetchWar(clanTag);
            if (runTimeWar != null)
                await ProcessWar(runTimeWar);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
    }

    private async Task ProcessWar(RunTimeWar runTimeWar)
    {
        if (runTimeWar.Clan == null || runTimeWar.Opponent == null)
        {
            Console.WriteLine("No war detected...");
            return;
        }

        switch (runTimeWar.State)
        {
            case "preparation":
            case "inWar":
                Console.WriteLine($"Tracking war: {runTimeWar.Clan.Name} vs {runTimeWar.Opponent.Name}, Status: {ClashConstants.WarStatus[runTimeWar.State.ToUpper()]}");
                break;
            default:
                Console.WriteLine($"{runTimeWar.Clan.Name} is currently not in war");
                break;
        }
        Console.WriteLine("Updated: " + DateTime.Now);

        if (runTimeWar.State == "preparation" && manager.IsNewWar(runTimeWar.PreparationStartTime))
        {
            Console.WriteLine("New war detected, resetting war files...");
            manager.ResetWarFiles();
            manager.SaveWarId(runTimeWar.PreparationStartTime);
        }

        await ProcessTeam(clan: runTimeWar.Clan, team: "home", teamFolder: ClashConstants.HomeFolder);
        await ProcessTeam(clan: runTimeWar.Opponent, team: "away", teamFolder: ClashConstants.AwayFolder);
    }

    private async Task ProcessTeam(RunTimeClan clan, string team, string teamFolder)
    {
        byte[] logo = await client.GetByteArrayAsync(clan.BadgeUrls.Large);
        manager.CreateTeamLogoFile(logo, team);
        await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} name.txt", clan.Name);
        await manager.WritePlayerNames(clan, teamFolder);

        await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} score.txt", clan.Stars.ToString());
        string percentage = Math.Round(clan.DestructionPercentage, 2).ToString("F2");
        await writer.WriteAsync($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} percentage.txt", percentage + "%");

        foreach (RunTimeMember member in clan.Members)
        {
            string pictureName = teamFolder + $"{Path.DirectorySeparatorChar}hit " + member.MapPosition + ".png";
            if (member.Attacks == null) continue;
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

            double minutes = member.Attacks.First().Duration / 60;
            double seconds = member.Attacks.First().Duration % 60;

            fileName = teamFolder + $"{Path.DirectorySeparatorChar}hit " + member.MapPosition + " time.txt";
            await writer.WriteAsync(fileName, $"{minutes}:{seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}");
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

    public async void UpdatePlayerTag(string playerTag)
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
            Console.WriteLine(e.StackTrace);
        }
    }
}
