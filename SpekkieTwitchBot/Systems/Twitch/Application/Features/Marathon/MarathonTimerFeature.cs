using EventTimerService;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;

public sealed class MarathonTimerFeature(
    IEventTimerService timer,
    IMarathonTimeCalculator calculator,
    ITwitchChat chat,
    Logger logger,
    IFeatureFlagService featureFlags)
{
    public async Task HandleSubAsync(SubHappened e, CancellationToken ct)
    {
        if (!featureFlags.IsEnabled("Marathon")) return;
        TimeSpan added = calculator.CalculateForSub(e);
        if (added <= TimeSpan.Zero) return;

        timer.AddTime(added);

        string who = e.Kind == SubKind.CommunityGift
            ? e.GifterUserName ?? "iemand"
            : e.RecipientUserName;

        await chat.SendAsync(FormatAddedMessage(who, added), ct);
        logger.LogInfo($"[MarathonTimer] +{added.TotalMinutes:0.##} min via {e.Kind} (Tier {e.Tier}) by {who}");
    }

    public async Task HandleBitsAsync(BitsHappened e, CancellationToken ct)
    {
        if (!featureFlags.IsEnabled("Marathon")) return;
        TimeSpan added = calculator.CalculateForBits(e.Bits);
        if (added <= TimeSpan.Zero) return;

        timer.AddTime(added);

        string who = e.IsAnonymous ? "anoniem" : e.UserName;
        await chat.SendAsync(FormatAddedMessage(who, added), ct);
        logger.LogInfo($"[MarathonTimer] +{added.TotalMinutes:0.##} min via {e.Bits} bits by {who}");
    }

    public async Task HandleDonationAsync(string userName, decimal euros, CancellationToken ct)
    {
        if (!featureFlags.IsEnabled("Marathon")) return;
        TimeSpan added = calculator.CalculateForDonation(euros);
        if (added <= TimeSpan.Zero) return;

        timer.AddTime(added);

        await chat.SendAsync(FormatAddedMessage(userName, added), ct);
        logger.LogInfo($"[MarathonTimer] +{added.TotalMinutes:0.##} min via €{euros} donatie by {userName}");
    }

    private static string FormatAddedMessage(string who, TimeSpan added)
    {
        string formatted = added.TotalHours >= 1
            ? $"{(int)added.TotalHours}u {added.Minutes:00}m"
            : added.TotalMinutes >= 1
                ? $"{(int)added.TotalMinutes}m {added.Seconds:00}s"
                : $"{(int)added.TotalSeconds}s";

        return $"@{who} +{formatted} op de marathon timer!";
    }
}
