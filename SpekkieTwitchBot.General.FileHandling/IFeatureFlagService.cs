namespace SpekkieTwitchBot.General.FileHandling;

public interface IFeatureFlagService
{
    bool IsEnabled(string flag);
}
