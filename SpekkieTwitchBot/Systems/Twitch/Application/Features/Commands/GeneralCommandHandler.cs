using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class GeneralCommandHandler(
    GeneralFileReader generalFileReader,
    GeneralFileWriter generalFileWriter,
    ITextCommandHandler textCommandHandler,
    ISpotifyCommandHandler spotifyCommandHandler,
    IObsCommandHandler obsCommandHandler,
    ITimerCommandHandler timerCommandHandler,
    ITwitchCommandHandler twitchCommandHandler,
    IClashCommandHandler clashCommandHandler,
    ITwitchFileReader twitchFileReader)
    : IGeneralCommandHandler
{
    public async Task<string> HandleCommand(ChatCommandReceived command, CancellationToken ct)
    {
        string username = command.Username;
        string commandText = $"!{command.CommandText}";
        string commandArgs = command.ArgumentsAsString ?? "";

        Dictionary<string, Func<CancellationToken, Task<string>>> commandHandlers = null!;
        commandHandlers = new Dictionary<string, Func<CancellationToken, Task<string>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["!commands"] = _ => Task.FromResult(HandleCommandsCommand()),
            ["!afgeleid"] = _ => Task.FromResult(HandleAfgeleidCommand()),

            ["!song"]        = spotifyCommandHandler.HandleGetCurrentSongCommand,
            ["!playlist"]    = spotifyCommandHandler.HandleGetCurrentPlaylistCommand,
            ["!pausemusic"]  = spotifyCommandHandler.HandlePauseMusicCommand,
            ["!resumemusic"] = spotifyCommandHandler.HandleResumeMusicCommand,
            ["!next"]       = spotifyCommandHandler.HandleNextSongCommand,
            ["!prev"]       = spotifyCommandHandler.HandlePrevSongCommand,
            ["!queue"]      = spotifyCommandHandler.HandleGetQueueCommand,
            ["!sr"]         = t => spotifyCommandHandler.HandleSongRequestCommand(commandArgs, command.UserId, username, t),
            ["!playsong"]   = t => spotifyCommandHandler.HandlePlaySpecificSongCommand(commandArgs, username, t),

            ["!createredemption"] = _ => twitchCommandHandler.HandleCreateRedemptionCommand(commandArgs),
            ["!uptime"]           = twitchCommandHandler.HandleUptimeCommand,
            ["!clip"]             = twitchCommandHandler.HandleClipCommand,
            ["!so"]               = t => twitchCommandHandler.HandleShoutoutCommand(commandArgs, t),

            ["!setscene"]        = _ => Task.FromResult(obsCommandHandler.HandleSetSceneCommand(commandArgs)),
            ["!mutemic"]         = _ => Task.FromResult(obsCommandHandler.HandleSetInputMute("microphone")),
            ["!mutemusic"]       = _ => Task.FromResult(obsCommandHandler.HandleSetInputMute("spotify")),
            ["!standardvolumes"] = _ => Task.FromResult(obsCommandHandler.HandleSetStandardVolumes()),
            ["!volumezero"]      = _ => Task.FromResult(obsCommandHandler.HandleVolumeZero()),

            ["!pausetimer"] = _ => Task.FromResult(timerCommandHandler.HandlePauseTimerCommand()),
            ["!starttimer"] = _ => Task.FromResult(timerCommandHandler.HandleStartTimerCommand()),
            ["!addtime"]    = _ => Task.FromResult(timerCommandHandler.HandleAddTimeToTimerCommand(commandArgs)),
            ["!settime"]    = _ => Task.FromResult(timerCommandHandler.HandleSetTimeOnTimerCommand(commandArgs)),
            ["!marathon"]   = _ => timerCommandHandler.HandleMarathonCommand(commandArgs),

            ["!war"]          = _ => Task.FromResult(clashCommandHandler.HandleSetWarStatsCommand(commandArgs)),
            ["!setplayertag"] = _ => clashCommandHandler.HandleAddPlayerTagCommand(commandArgs),

            ["!subgoal"] = HandleSubGoalCommand,
            ["!subdoel"] = HandleSubGoalCommand,
        };

        if (!commandHandlers.TryGetValue(commandText, out Func<CancellationToken, Task<string>>? handler))
            return "Unknown command";

        return await handler(ct).ConfigureAwait(false);

        string HandleCommandsCommand()
        {
            List<string> all = textCommandHandler.GetTextCommands()
                .Select(x => x.Command ?? "")
                .Concat(commandHandlers.Keys)
                .ToList();
            return $"The following commands are available on this channel: {string.Join(", ", all)}";
        }
    }

    private async Task<string> HandleSubGoalCommand(CancellationToken ct)
    {
        StreamGoalsConfig? config = await twitchFileReader.ReadGoalsConfigAsync();
        if (config == null) return "No sub goal configured.";
        SubGoalConfig sub = config.SubGoal;
        int daysRemaining = Math.Max(0, sub.EndDate.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber);
        return $"Sub goal: {sub.CurrentAmount}/{sub.Goal} subs before {sub.EndDate:MMM d} ({daysRemaining} days left) — Reward: {sub.RewardEn} " +
               $"| Sub doel: {sub.CurrentAmount}/{sub.Goal} subs voor {sub.EndDate:d MMM} ({daysRemaining} dagen over) — Beloning: {sub.RewardNl}";
    }

    private string HandleAfgeleidCommand()
    {
        string afgeleidtext = generalFileReader.ReadAfgeleidCounter();
        if (!int.TryParse(afgeleidtext, out int afgeleid))
            afgeleid = 0;
        afgeleid++;
        generalFileWriter.WriteAfgeleidCounter(afgeleid.ToString());
        return $"Spekkie is {afgeleid}x afgeleid geweest";
    }
}