using SpekkieClassLibrary.ClashOfClans.War;

namespace SpekkieTwitchBot.General.FileHandling.Clash;

public class ClashFileManager
{
    private readonly ClashFileSetup _FileSetup;
    private readonly ClashFileWriter _FileWriter;
    
    private static readonly string BaseDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot";
    private static readonly string OutputDir = $"{BaseDir}/Output/ClashOfClans";
    private readonly string _HomeFolder;
    private readonly string _AwayFolder;
    
    
    public ClashFileManager(ClashFileSetup setup, ClashFileWriter writer)
    {
        _FileSetup = setup;
        _FileWriter = writer;
        
        _HomeFolder = $"{OutputDir}{Path.DirectorySeparatorChar}home";
        _AwayFolder = $"{OutputDir}{Path.DirectorySeparatorChar}away";
        SetupPaths();
    }

    public void SetupPaths()
    {
        SetupDocumentsFolder();
        SetupOutputFolder();
        SetupTeamFolders();
        Console.WriteLine("Setting up Clash Paths");
    }

    private void SetupDocumentsFolder()
    {
        if(!_FileSetup.DirExists(BaseDir))
            _FileSetup.CreateDir(BaseDir);

        if(!_FileSetup.DirExists(OutputDir))
            _FileSetup.CreateDir(OutputDir);
        
        string fileName = $"{OutputDir}{Path.DirectorySeparatorChar}player tag.txt";
        if(!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);       
        
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}clan tag.txt";
        if(!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);       
        
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}clash api token.txt";
        if(!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
    }

    private void SetupOutputFolder()
    {
        string fileName = $"{OutputDir}{Path.DirectorySeparatorChar}away avg time.txt";
        if (!_FileSetup.FileExists(fileName))
        {
            _FileSetup.CreateFile(fileName);   
            _FileWriter.Write(fileName, "0:00");
        }
        
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}away name.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);        
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}away percentage.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);        
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}away score.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);             
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}home avg time.txt";
        if (!_FileSetup.FileExists(fileName))
        {
            _FileSetup.CreateFile(fileName);    
            _FileWriter.Write(fileName, "0:00");
        }
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}home name.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);        
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}home percentage.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);        
        fileName = $"{OutputDir}{Path.DirectorySeparatorChar}home score.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);        
    }

    private void SetupTeamFolders()
    {
        if(!_FileSetup.DirExists(_HomeFolder))
            _FileSetup.CreateDir(_HomeFolder);
        
        if(!_FileSetup.DirExists(_AwayFolder))
            _FileSetup.CreateDir(_AwayFolder);

        for (int i = 1; i < 51; i++)
        {
            CreateHitFile(team: _HomeFolder, mapPosition: i, identifier: "name");
            CreateHitFile(team: _HomeFolder, mapPosition: i, identifier: "time");
            CreateHitFile(team: _HomeFolder, mapPosition: i, identifier: "hit");            
            CreateHitFile(team: _AwayFolder, mapPosition: i, identifier: "name");
            CreateHitFile(team: _AwayFolder, mapPosition: i, identifier: "time");
            CreateHitFile(team: _AwayFolder, mapPosition: i, identifier: "hit");
        }
    }

    private void CreateHitFile(string team, int mapPosition, string identifier)
    {
        string fileName = $"{team}{Path.DirectorySeparatorChar}{identifier} {mapPosition}.txt";
        if (!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
    }
    
    public void WritePlayerNames(RunTimeClan runTimeClan, string teamFolder)
    {
        foreach (RunTimeMember member in runTimeClan.Members.OrderBy(m => m.MapPosition))
        {
            string fileName = $"{teamFolder}{Path.DirectorySeparatorChar}name {member.MapPosition}.txt";
            if(!_FileSetup.FileExists(fileName))
                _FileSetup.CreateFile(fileName);
            _FileWriter.WritePlayerName(fileName, member.Name);
        }
    }

    public static void CreateTeamLogoFile(byte[] logo, string team)
    {
        var imgPath = $"{OutputDir}{Path.DirectorySeparatorChar}logo {team}.png";

        try
        {
            using var fileStream = new FileStream(imgPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            fileStream.Write(logo, 0, logo.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing to the file: {ex.Message}");
        }
        /*string fileName = $"{teamFolder}{Path.DirectorySeparatorChar}logo {team}.png";
        if(!_FileSetup.FileExists(fileName))
            _FileSetup.CreateFile(fileName);
        _FileWriter.CreateTeamLogo(fileName, logo);*/
    }
}