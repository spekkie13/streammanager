namespace SpekkieTwitchBot.General.FileHandling;

public interface IFeatureFlagService
{
    bool IsEnabled(string flag);
    Task SetEnabledAsync(string flag, bool value);
}
