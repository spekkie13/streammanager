using SpekkieClassLibrary.ClashOfClans.War;
using SpekkieTwitchBot.Systems.Overlay;

namespace SpekkieTwitchBot.Tests;

// Hit rate = share of attacks actually thrown that were triples (3 stars / 100%),
// NOT participation. See OverlayStateService.CalculateHitRate.
public class OverlayHitRateTests
{
    private static RunTimeMember MemberWith(params int[] stars) => new()
    {
        Attacks = stars.Select(s => new RunTimeAttack { Stars = s }).ToList()
    };

    private static RunTimeClan ClanWith(params RunTimeMember[] members) => new()
    {
        Members = members.ToList()
    };

    [Fact]
    public void AllTriples_Returns100()
    {
        RunTimeClan clan = ClanWith(MemberWith(3), MemberWith(3), MemberWith(3));

        Assert.Equal(100, OverlayStateService.CalculateHitRate(clan));
    }

    [Fact]
    public void NoTriples_Returns0()
    {
        RunTimeClan clan = ClanWith(MemberWith(1), MemberWith(2), MemberWith(0));

        Assert.Equal(0, OverlayStateService.CalculateHitRate(clan));
    }

    // 2 triples out of 4 thrown attacks = 50% — denominator is attacks made, not team size.
    [Fact]
    public void HalfTriples_Returns50()
    {
        RunTimeClan clan = ClanWith(MemberWith(3), MemberWith(2), MemberWith(3), MemberWith(1));

        Assert.Equal(50, OverlayStateService.CalculateHitRate(clan));
    }

    // Only the 2 thrown attacks count; the two players who never attacked do not
    // drag the accuracy down. 1 triple / 2 attacks = 50%.
    [Fact]
    public void UnusedAttacks_ExcludedFromDenominator()
    {
        RunTimeClan clan = ClanWith(
            MemberWith(3),
            MemberWith(1),
            new RunTimeMember { Attacks = null },
            new RunTimeMember { Attacks = new List<RunTimeAttack>() });

        Assert.Equal(50, OverlayStateService.CalculateHitRate(clan));
    }

    [Fact]
    public void NoAttacksAtAll_Returns0()
    {
        RunTimeClan clan = ClanWith(
            new RunTimeMember { Attacks = null },
            new RunTimeMember { Attacks = new List<RunTimeAttack>() });

        Assert.Equal(0, OverlayStateService.CalculateHitRate(clan));
    }

    // A "notInWar" placeholder from the CoC API has a clan block with a null Members list. This used to
    // throw ArgumentNullException every 5 seconds out of the overlay writer loop.
    [Fact]
    public void NullMembers_Returns0()
    {
        RunTimeClan clan = new() { Members = null };

        Assert.Equal(0, OverlayStateService.CalculateHitRate(clan));
    }

    // 1 triple / 3 attacks = 33.33% rounds to 33.
    [Fact]
    public void Rounds_ToNearestWholePercent()
    {
        RunTimeClan clan = ClanWith(MemberWith(3), MemberWith(2), MemberWith(1));

        Assert.Equal(33, OverlayStateService.CalculateHitRate(clan));
    }
}
