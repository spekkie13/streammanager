using Newtonsoft.Json;
using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling.Common;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class CommandPermissionService : ICommandPermissionService
{
    private static readonly string FilePath = Path.Combine(BotPaths.BaseDir, "Settings", "CommandPermissions.json");

    public bool IsAllowed(string command, UserRole userRole)
    {
        Dictionary<string, string> permissions = LoadPermissions();

        if (!permissions.TryGetValue(command, out string? requiredRoleStr))
            return true; // not configured = open to everyone

        if (!Enum.TryParse(requiredRoleStr, ignoreCase: true, out UserRole requiredRole))
            return true; // unrecognised value in config = open to everyone

        return userRole >= requiredRole;
    }

    private static Dictionary<string, string> LoadPermissions()
    {
        if (!File.Exists(FilePath))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using FileStream fs = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader sr = new(fs);
        string json = sr.ReadToEnd();

        CommandPermissionsConfig? config = JsonConvert.DeserializeObject<CommandPermissionsConfig>(json);
        return config?.Permissions ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class CommandPermissionsConfig
    {
        [JsonProperty("Permissions")]
        public Dictionary<string, string> Permissions { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
