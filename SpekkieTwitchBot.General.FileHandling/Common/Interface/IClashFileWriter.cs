namespace SpekkieTwitchBot.General.FileHandling.Common.Interface;

public interface IClashFileWriter : ITextFileWriter
{
    Task WriteTeamNames(string file, string teamName);
    void CreateTeamLogo(string file, byte[] data);
}
