using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.StreamElements;

public sealed class StreamElementsHostedService : IHostedService
{
    private readonly StreamElementsClient _client;
    private readonly MarathonTimerFeature _marathon;
    private readonly Logger _logger;
    private CancellationTokenSource? _cts;

    public StreamElementsHostedService(
        StreamElementsClient client,
        MarathonTimerFeature marathon,
        Logger logger)
    {
        _client = client;
        _marathon = marathon;
        _logger = logger;

        _client.OnDonation += HandleDonation;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = Task.Run(() => _client.ConnectAsync(_cts.Token), CancellationToken.None);
        _logger.LogInfo("[StreamElements] Hosted service starting");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInfo("[StreamElements] Hosted service stopping");
        _client.OnDonation -= HandleDonation;
        try { _cts?.Cancel(); } catch { /* ignore */ }
        await _client.DisconnectAsync(ct).ConfigureAwait(false);
        try { _cts?.Dispose(); } catch { /* ignore */ }
        _cts = null;
    }

    private Task HandleDonation(DonationHappened e, CancellationToken ct)
        => _marathon.HandleDonationAsync(e.UserName, e.Amount, ct);
}
