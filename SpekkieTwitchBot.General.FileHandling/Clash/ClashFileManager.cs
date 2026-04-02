using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieClassLibrary.Constants;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;

namespace SpekkieTwitchBot.General.FileHandling.Clash;

public class ClashFileManager
{
    private readonly FileSetup _FileSetup;
    private readonly IClashFileWriter _FileWriter;

    public ClashFileManager(FileSetup setup, IClashFileWriter writer)
    {
        _FileSetup = setup;
        _FileWriter = writer;
        SetupPaths();
    }

    public void SetupPaths()
    {
        Console.WriteLine("Setting up Clash Paths");
        SetupDocumentsFolder();
        SetupOutputFolder();
        SetupTeamFolders();
    }

    private void SetupDocumentsFolder()
    {
        if (!_FileSetup.DirExists(ClashConstants.BaseDir))
            _FileSetup.CreateDir(ClashConstants.BaseDir);

        if (!_FileSetup.DirExists(ClashConstants.OutputDir))
            _FileSetup.CreateDir(ClashConstants.OutputDir);

        string fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}player tag.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);

        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clan tag.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);

        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}clash api token.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);

        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}current war id.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);

        _FileWriter.Write(
            $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}war-stats-overlay.html",
            WarOverlayHtml.Content);
    }

    private void SetupOutputFolder()
    {
        string fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}away avg time.txt";
        if (!_FileSetup.FileExists(fileName))
        {
            _FileSetup.CreateFile(fileName);
            _FileWriter.Write(fileName, "0:00");
        }

        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}away name.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}away percentage.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}away score.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}home avg time.txt";
        if (!_FileSetup.FileExists(fileName))
        {
            _FileSetup.CreateFile(fileName);
            _FileWriter.Write(fileName, "0:00");
        }
        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}home name.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}home percentage.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
        fileName = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}home score.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
    }

    private void SetupTeamFolders()
    {
        if (!_FileSetup.DirExists(ClashConstants.HomeFolder))
            _FileSetup.CreateDir(ClashConstants.HomeFolder);

        if (!_FileSetup.DirExists(ClashConstants.AwayFolder))
            _FileSetup.CreateDir(ClashConstants.AwayFolder);

        for (int i = 1; i < 51; i++)
        {
            CreateHitFile(team: ClashConstants.HomeFolder, mapPosition: i, identifier: "name");
            CreateHitFile(team: ClashConstants.HomeFolder, mapPosition: i, identifier: "time");
            CreateHitFile(team: ClashConstants.HomeFolder, mapPosition: i, identifier: "hit");
            CreateHitFile(team: ClashConstants.AwayFolder, mapPosition: i, identifier: "name");
            CreateHitFile(team: ClashConstants.AwayFolder, mapPosition: i, identifier: "time");
            CreateHitFile(team: ClashConstants.AwayFolder, mapPosition: i, identifier: "hit");
        }
    }

    private void CreateHitFile(string team, int mapPosition, string identifier)
    {
        string fileName = $"{team}{Path.DirectorySeparatorChar}{identifier} {mapPosition}.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
    }

    public bool IsNewWar(string preparationStartTime)
    {
        string warIdFile = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}current war id.txt";
        string storedId = File.ReadAllText(warIdFile).Trim();
        return storedId != preparationStartTime;
    }

    public void SaveWarId(string preparationStartTime)
    {
        string warIdFile = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}current war id.txt";
        _FileWriter.Write(warIdFile, preparationStartTime);
    }

    public void ResetWarFiles()
    {
        string unhitBase = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}BaseImages{Path.DirectorySeparatorChar}Unhit_Base.png";
        foreach (string teamFolder in new[] { ClashConstants.HomeFolder, ClashConstants.AwayFolder })
        {
            for (int i = 1; i <= 50; i++)
            {
                File.Copy(unhitBase, $"{teamFolder}{Path.DirectorySeparatorChar}hit {i}.png", overwrite: true);
                _FileWriter.Write($"{teamFolder}{Path.DirectorySeparatorChar}hit {i}.txt", "");
                _FileWriter.Write($"{teamFolder}{Path.DirectorySeparatorChar}hit {i} time.txt", "");
                _FileWriter.Write($"{teamFolder}{Path.DirectorySeparatorChar}name {i}.txt", "");
            }
        }
    }

    public async Task WritePlayerNames(RunTimeClan runTimeClan, string teamFolder)
    {
        foreach (RunTimeMember member in runTimeClan.Members.OrderBy(m => m.MapPosition))
        {
            string fileName = $"{teamFolder}{Path.DirectorySeparatorChar}name {member.MapPosition}.txt";
            if (!_FileSetup.FileExists(fileName))
                _FileSetup.CreateFile(fileName);
            await _FileWriter.WriteTeamNames(fileName, member.Name);
        }
    }

    public void CreateTeamLogoFile(byte[] logo, string team)
    {
        string imgPath = $"{ClashConstants.OutputDir}{Path.DirectorySeparatorChar}logo {team}.png";
        _FileWriter.CreateTeamLogo(imgPath, logo);
    }
}