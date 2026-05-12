using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.StreamElements;

public sealed class StreamElementsHostedService : IHostedService
{
    private readonly StreamElementsClient _Client;
    private readonly MarathonTimerFeature _Marathon;
    private readonly Logger _Logger;
    private CancellationTokenSource? _Cts;

    public StreamElementsHostedService(
        StreamElementsClient client,
        MarathonTimerFeature marathon,
        Logger logger)
    {
        _Client = client;
        _Marathon = marathon;
        _Logger = logger;

        _Client.OnDonation += HandleDonation;
    }

    public Task StartAsync(CancellationToken ct)
    {
        _Cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = Task.Run(() => _Client.ConnectAsync(_Cts.Token), CancellationToken.None);
        _Logger.LogInfo("[StreamElements] Hosted service starting");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _Logger.LogInfo("[StreamElements] Hosted service stopping");
        _Client.OnDonation -= HandleDonation;
        try { _Cts?.Cancel(); } catch { /* ignore */ }
        await _Client.DisconnectAsync(ct).ConfigureAwait(false);
        try { _Cts?.Dispose(); } catch { /* ignore */ }
        _Cts = null;
    }

    private Task HandleDonation(DonationHappened e, CancellationToken ct)
        => _Marathon.HandleDonationAsync(e.UserName, e.Amount, ct);
}
