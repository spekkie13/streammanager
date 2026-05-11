using SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Tests;

public class MarathonTimeCalculatorTests
{
    private readonly MarathonTimeCalculator _calc = new();

    // --- Bits: spec-voorbeelden ---

    [Fact]
    public void Bits_750_ReturnsSpecExample()
    {
        // 1× 500-drempel (420s) + 250/100 × 77s = 420 + 192.5 = 612.5s
        TimeSpan result = _calc.CalculateForBits(750);
        Assert.Equal(612.5, result.TotalSeconds, precision: 4);
    }

    [Fact]
    public void Bits_ExactThreshold_100_Returns77Seconds()
    {
        TimeSpan result = _calc.CalculateForBits(100);
        Assert.Equal(77, result.TotalSeconds, precision: 4);
    }

    [Fact]
    public void Bits_ExactThreshold_500_Returns7Minutes()
    {
        TimeSpan result = _calc.CalculateForBits(500);
        Assert.Equal(7, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void Bits_ExactThreshold_10000_Returns140Minutes()
    {
        TimeSpan result = _calc.CalculateForBits(10_000);
        Assert.Equal(140, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void Bits_Zero_ReturnsZero()
    {
        Assert.Equal(TimeSpan.Zero, _calc.CalculateForBits(0));
    }

    // --- Gifted subs: spec-voorbeelden ---

    [Fact]
    public void GiftedSubs_15Tier1_ReturnsSpecExample()
    {
        // 1× 10-drempel (84min) + 1× 5-drempel (42min) = 126 min
        TimeSpan result = _calc.CalculateForSub(MakeCommunityGift(15, "1000"));
        Assert.Equal(126, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void GiftedSubs_5Tier2_ReturnsSpecExample()
    {
        // 5 × factor 2 = 10 equiv → 1× 10-drempel = 84 min
        TimeSpan result = _calc.CalculateForSub(MakeCommunityGift(5, "2000"));
        Assert.Equal(84, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void GiftedSubs_3Tier2_ReturnsSpecExample()
    {
        // 3 × factor 2 = 6 equiv → 1× 5-drempel (42min) + 1× 1-drempel (7min) = 49 min
        TimeSpan result = _calc.CalculateForSub(MakeCommunityGift(3, "2000"));
        Assert.Equal(49, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void GiftedSubs_1Tier3_ReturnsTierFactorApplied()
    {
        // 1 × factor 4 = 4 equiv → 4× 1-drempel = 28 min
        TimeSpan result = _calc.CalculateForSub(MakeCommunityGift(1, "3000"));
        Assert.Equal(28, result.TotalMinutes, precision: 4);
    }

    // --- Individuele subs: tier-factoren ---

    [Fact]
    public void Sub_NewTier1_Returns7Minutes()
    {
        TimeSpan result = _calc.CalculateForSub(MakeSub(SubKind.New, "1000"));
        Assert.Equal(7, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void Sub_NewTier2_Returns14Minutes()
    {
        TimeSpan result = _calc.CalculateForSub(MakeSub(SubKind.New, "2000"));
        Assert.Equal(14, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void Sub_NewTier3_Returns28Minutes()
    {
        TimeSpan result = _calc.CalculateForSub(MakeSub(SubKind.New, "3000"));
        Assert.Equal(28, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void Sub_Prime_Returns7Minutes()
    {
        TimeSpan result = _calc.CalculateForSub(MakeSub(SubKind.New, "prime"));
        Assert.Equal(7, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void Sub_Resub_AddsTime()
    {
        TimeSpan result = _calc.CalculateForSub(MakeSub(SubKind.Resub, "1000"));
        Assert.Equal(7, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void Sub_PrimePaidUpgrade_Returns7Minutes()
    {
        TimeSpan result = _calc.CalculateForSub(MakeSub(SubKind.PrimePaidUpgrade, "1000"));
        Assert.Equal(7, result.TotalMinutes, precision: 4);
    }

    [Fact]
    public void Sub_GiftKind_ReturnsZero()
    {
        // SubKind.Gift wordt in de praktijk nooit gefired (gefilterd in EventSubClient)
        TimeSpan result = _calc.CalculateForSub(MakeSub(SubKind.Gift, "1000"));
        Assert.Equal(TimeSpan.Zero, result);
    }

    // --- Donaties ---

    [Fact]
    public void Donation_ExactThreshold_5Euro_Returns7Minutes()
    {
        Assert.Equal(7, _calc.CalculateForDonation(5m).TotalMinutes, precision: 4);
    }

    [Fact]
    public void Donation_ExactThreshold_100Euro_Returns140Minutes()
    {
        Assert.Equal(140, _calc.CalculateForDonation(100m).TotalMinutes, precision: 4);
    }

    [Fact]
    public void Donation_Fractional_7Euro50_ReturnsBetween7And14Minutes()
    {
        // 7.50 / 5 = 1.5× laagste drempel → 1.5 × 420s = 630s = 10.5 min
        double result = _calc.CalculateForDonation(7.50m).TotalMinutes;
        Assert.Equal(10.5, result, precision: 4);
    }

    [Fact]
    public void Donation_Zero_ReturnsZero()
    {
        Assert.Equal(TimeSpan.Zero, _calc.CalculateForDonation(0m));
    }

    // --- Helpers ---

    private static SubHappened MakeSub(SubKind kind, string tier) => new(
        Kind: kind,
        RecipientUserId: "u1",
        RecipientUserName: "user",
        GifterUserId: null,
        GifterUserName: null,
        Tier: tier,
        IsPrime: tier == "prime",
        TotalMonths: null,
        GiftCount: 0,
        Message: null,
        Timestamp: DateTimeOffset.UtcNow);

    private static SubHappened MakeCommunityGift(int count, string tier) => new(
        Kind: SubKind.CommunityGift,
        RecipientUserId: "",
        RecipientUserName: "(community)",
        GifterUserId: "g1",
        GifterUserName: "gifter",
        Tier: tier,
        IsPrime: false,
        TotalMonths: null,
        GiftCount: count,
        Message: null,
        Timestamp: DateTimeOffset.UtcNow);
}
