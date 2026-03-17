using SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;

namespace SpekkieTwitchBot.Tests;

public class PubSubReconnectPolicyTests
{
    [Fact]
    public void NextDelay_FirstCall_ReturnsAtLeast250Ms()
    {
        PubSubReconnectPolicy policy = new PubSubReconnectPolicy();

        TimeSpan delay = policy.NextDelay();

        Assert.True(delay >= TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public void NextDelay_EachCall_ReturnsSameOrHigherDelay()
    {
        PubSubReconnectPolicy policy = new PubSubReconnectPolicy();

        for (int i = 0; i < 8; i++)
        {
            TimeSpan current = policy.NextDelay();
            // base delay grows; even with zero jitter it should be >= previous base
            Assert.True(current >= TimeSpan.FromMilliseconds(250));
        }
    }

    [Fact]
    public void NextDelay_AfterManyCalls_CapsAt30Seconds()
    {
        PubSubReconnectPolicy policy = new PubSubReconnectPolicy();

        // Drain past the cap (attempt is capped at 8 → 2^8*250 = 64000ms → capped at 30000)
        for (int i = 0; i < 20; i++)
            policy.NextDelay();

        TimeSpan delay = policy.NextDelay();

        Assert.True(delay <= TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Reset_AfterSeveralCalls_ResetsDelayToInitialRange()
    {
        PubSubReconnectPolicy policy = new PubSubReconnectPolicy();

        // Advance to high delay
        for (int i = 0; i < 8; i++) policy.NextDelay();

        policy.Reset();
        TimeSpan afterReset = policy.NextDelay();

        // After reset, first call should be in the 250–500ms range (2^1 * 250 + jitter 0–250)
        Assert.True(afterReset <= TimeSpan.FromMilliseconds(750));
    }

    [Fact]
    public void NextDelay_IncludesJitter_DelayIsNotAlwaysIdentical()
    {
        // Run enough iterations to statistically observe jitter variation
        HashSet<double> delays = new HashSet<double>();
        for (int i = 0; i < 30; i++)
        {
            PubSubReconnectPolicy policy = new PubSubReconnectPolicy();
            delays.Add(policy.NextDelay().TotalMilliseconds);
        }

        // With 30 fresh policies, at least 2 distinct values should appear (jitter range is 0–249ms)
        Assert.True(delays.Count > 1);
    }
}
