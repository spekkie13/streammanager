using System.Globalization;
using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Constants;
using SpekkieTwitchBot.General.FileHandling.Clash;

namespace SpekkieTwitchBot.ClashOfClans.StatsBot;

public class WarService : BackgroundService
{
    private readonly WarStatus _WarStatus;
    private readonly ClashFileReader _FileReader;
    private readonly ClashFileWriter _FileWriter;
    private readonly ClashFileManager _FileManager;
    private readonly CocHttpClient _CocHttpClient;
    
    public WarService(ClashFileReader reader, ClashFileWriter writer, ClashFileManager manager, CocHttpClient client, WarStatus warStatus)
    {
        _WarStatus = warStatus;
        _FileManager = manager;
        _FileReader = reader;
        _FileWriter = writer;
        _CocHttpClient = client;
        
        manager.SetupPaths();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_WarStatus.GetStatus())
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
            //TODO: Clean previous war result in file structure
            bool status = _WarStatus.GetStatus();
            _WarStatus.SetStatus(!status);
            Console.WriteLine($"War stats active: {!status}");
            string playerFile = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}player tag.txt";
            string playerTag = _FileReader.ReadPlayerTag(playerFile);
            string clanTag = await _CocHttpClient.GetPlayerClan(playerTag);
            _FileWriter.Write($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clan tag.txt", clanTag);
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
            string clanTag = _FileReader.Read(clanTagPath);
            clanTag = clanTag.Replace("\r", "").Replace("\n", "");
            if (string.IsNullOrEmpty(clanTag))
            {
                Console.WriteLine("Clan tag is empty");
                return;
            }

            RunTimeWar? runTimeWar = await _CocHttpClient.FetchWar(clanTag);
            if(runTimeWar != null)
                ProcessWar(runTimeWar);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
    }
    
    private void ProcessWar(RunTimeWar runTimeWar)
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
        
        ProcessTeam(clan: runTimeWar.Clan, team: "home", teamFolder: ClashConstants.HomeFolder);
        ProcessTeam(clan: runTimeWar.Opponent, team: "away", teamFolder: ClashConstants.AwayFolder);
    }

    private void ProcessTeam(RunTimeClan clan, string team, string teamFolder)
    {
        byte[] logo = _CocHttpClient.GetByteArrayAsync(clan.BadgeUrls.Large).Result;
        ClashFileManager.CreateTeamLogoFile(logo, team);
        _FileWriter.WriteTeamNames($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} name.txt", clan.Name);
        _FileManager.WritePlayerNames(clan, teamFolder);
        
        _FileWriter.Write($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} score.txt", clan.Stars.ToString());
        string percentage = Math.Round(clan.DestructionPercentage, 2).ToString("F2");
        _FileWriter.Write($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} percentage.txt", percentage + "%");
        
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
            _FileWriter.Write(fileName, member.Attacks.First().DestructionPercentage + "%");

            double minutes = member.Attacks.First().Duration / 60;
            double seconds = member.Attacks.First().Duration % 60;

            fileName = teamFolder + $"{Path.DirectorySeparatorChar}hit " + member.MapPosition + " time.txt";
            _FileWriter.Write(fileName, $"{minutes}:{seconds.ToString(CultureInfo.InvariantCulture).PadLeft(2,'0')}");
        }
        
        List<double> times = (from member in clan.Members 
            from attack in member.Attacks ?? Enumerable.Empty<RunTimeAttack>() 
            select attack.Duration).ToList();

        double avgTime = 0;
        if (times.Count > 0)
            avgTime = times.Average();

        int sec = (int) avgTime % 60;
        int min = (int) avgTime / 60;

        string averageTime = $"{min}:{sec.ToString().PadLeft(2, '0')}";
        _FileWriter.Write($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}{team} avg time .txt", averageTime);
    }

    public bool GetWarStatus()
    {
        return _WarStatus.GetStatus();
    }
    
    public async void UpdatePlayerTag(string playerTag)
    {
        try
        {
            string playerFile = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}player tag.txt";
            _FileWriter.Write(playerFile, playerTag);
            string clanTag = await _CocHttpClient.GetPlayerClan(playerTag);
            _FileWriter.Write($"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clan tag.txt", clanTag);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.StackTrace);
        }
    }
}