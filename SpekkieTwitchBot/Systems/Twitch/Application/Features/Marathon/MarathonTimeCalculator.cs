using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;

public sealed class MarathonTimeCalculator : IMarathonTimeCalculator
{
    // (drempelwaarde, seconden toegevoegd) — hoog naar laag
    private static readonly (double Threshold, double Seconds)[] GiftedSubThresholds =
    [
        (50, 420 * 60),
        (20, 175 * 60),
        (10,  84 * 60),
        (5,   42 * 60),
        (1,    7 * 60),
    ];

    private static readonly (double Threshold, double Seconds)[] BitsThresholds =
    [
        (10_000, 140 * 60),
        (5_000,   77 * 60),
        (2_500,   49 * 60),
        (1_000,   13 * 60),
        (500,      7 * 60),
        (100,         77),
    ];

    private static readonly (double Threshold, double Seconds)[] DonationThresholds =
    [
        (100, 140 * 60),
        (50,   77 * 60),
        (20,   28 * 60),
        (10,   14 * 60),
        (5,     7 * 60),
    ];

    public TimeSpan CalculateForSub(SubHappened sub)
    {
        return sub.Kind switch
        {
            SubKind.New or SubKind.Resub or SubKind.PrimePaidUpgrade or SubKind.ContinuedGift
                => TimeSpan.FromSeconds(TierFactor(sub.Tier) * 7 * 60),
            SubKind.CommunityGift
                => BPlus(sub.GiftCount * TierFactor(sub.Tier), GiftedSubThresholds),
            _ => TimeSpan.Zero
        };
    }

    public TimeSpan CalculateForBits(int bits)
        => BPlus(bits, BitsThresholds);

    public TimeSpan CalculateForDonation(decimal euros)
        => BPlus((double)euros, DonationThresholds);

    private static int TierFactor(string tier) => tier switch
    {
        "2000" => 2,
        "3000" => 4,
        _ => 1
    };

    // B+ algoritme: floor-deling per drempel, fractioneel op de laagste
    private static TimeSpan BPlus(double amount, (double Threshold, double Seconds)[] thresholds)
    {
        double totalSeconds = 0;
        double remaining = amount;

        for (int i = 0; i < thresholds.Length - 1; i++)
        {
            double times = Math.Floor(remaining / thresholds[i].Threshold);
            totalSeconds += times * thresholds[i].Seconds;
            remaining -= times * thresholds[i].Threshold;
        }

        (double lowestThreshold, double lowestSeconds) = thresholds[^1];
        totalSeconds += remaining / lowestThreshold * lowestSeconds;

        return TimeSpan.FromSeconds(totalSeconds);
    }
}
