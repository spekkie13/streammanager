using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features;

// Accumulates the cumulative bits cheered and euros donated and exposes them as overlay counters.
// These run alongside MarathonTimerFeature (which consumes the same events for the timer) — this
// feature only tracks running totals; it has no goal/perk concept, unlike the sub goal.
public sealed class SupportTotalsFeature(
    ITwitchFileWriter files,
    ITwitchFileReader fileReader,
    Logger logger)
{
    // Serializes the read-modify-write of the totals so a bit cheer and a donation arriving together
    // can't clobber each other's increment.
    private readonly SemaphoreSlim _lock = new(1, 1);

    // The current totals as a single immutable record, swapped under _lock. Held behind Volatile so the
    // overlay writer loop (another thread) reads a consistent snapshot without tearing.
    private SupportTotals _totals = new(0, 0);

    // Latest totals for read-only consumers such as the overlay state writer.
    public SupportTotals Snapshot => Volatile.Read(ref _totals);

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        SupportTotals? totals = await fileReader.ReadSupportTotalsAsync();
        await _lock.WaitAsync(ct);
        try
        {
            _totals = totals ?? new SupportTotals(0, 0);
            Persist();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task HandleBitsAsync(BitsHappened e, CancellationToken ct = default)
    {
        if (e.Bits <= 0) return;
        await _lock.WaitAsync(ct);
        try
        {
            _totals = _totals with { BitsTotal = _totals.BitsTotal + e.Bits };
            Persist();
            logger.LogInfo($"[SupportTotals] +{e.Bits} bits (total {_totals.BitsTotal})");
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task HandleDonationAsync(decimal euros, CancellationToken ct = default)
    {
        if (euros <= 0) return;
        await _lock.WaitAsync(ct);
        try
        {
            _totals = _totals with { DonationTotal = _totals.DonationTotal + euros };
            Persist();
            logger.LogInfo($"[SupportTotals] +€{euros} donated (total €{_totals.DonationTotal})");
        }
        finally
        {
            _lock.Release();
        }
    }

    private void Persist()
    {
        SupportTotals totals = _totals;
        files.WriteSupportTotals(totals);
        files.WriteSupportTotalsHtml(totals);
    }
}
