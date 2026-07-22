using Moq;
using SpekkieClassLibrary.Twitch;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Tests;

public class SupportTotalsFeatureTests
{
    private readonly Mock<ITwitchFileWriter> _Files = new();
    private readonly Mock<ITwitchFileReader> _FileReader = new();
    private readonly Mock<Logger> _Logger = new(MockBehavior.Loose, null!);

    private SupportTotalsFeature CreateFeature() =>
        new(_Files.Object, _FileReader.Object, _Logger.Object);

    private static BitsHappened Bits(int amount) =>
        new("uid1", "viewer1", false, amount, null, DateTimeOffset.UtcNow);

    [Fact]
    public async Task HandleBits_AddsToTotalAndPersists()
    {
        SupportTotals? written = null;
        _Files.Setup(f => f.WriteSupportTotals(It.IsAny<SupportTotals>()))
              .Callback<SupportTotals>(t => written = t);

        await CreateFeature().HandleBitsAsync(Bits(150), CancellationToken.None);

        Assert.NotNull(written);
        Assert.Equal(150, written!.BitsTotal);
    }

    [Fact]
    public async Task HandleDonation_AddsToTotalAndPersists()
    {
        SupportTotals? written = null;
        _Files.Setup(f => f.WriteSupportTotals(It.IsAny<SupportTotals>()))
              .Callback<SupportTotals>(t => written = t);

        await CreateFeature().HandleDonationAsync(12.50m, CancellationToken.None);

        Assert.NotNull(written);
        Assert.Equal(12.50m, written!.DonationTotal);
    }

    [Fact]
    public async Task Handlers_Accumulate_AcrossEvents()
    {
        SupportTotals? written = null;
        _Files.Setup(f => f.WriteSupportTotals(It.IsAny<SupportTotals>()))
              .Callback<SupportTotals>(t => written = t);

        SupportTotalsFeature feature = CreateFeature();
        await feature.HandleBitsAsync(Bits(100), CancellationToken.None);
        await feature.HandleBitsAsync(Bits(50), CancellationToken.None);
        await feature.HandleDonationAsync(5m, CancellationToken.None);
        await feature.HandleDonationAsync(2.5m, CancellationToken.None);

        Assert.Equal(150, written!.BitsTotal);
        Assert.Equal(7.5m, written.DonationTotal);
    }

    [Fact]
    public async Task Initialize_SeedsFromExistingTotals()
    {
        _FileReader.Setup(r => r.ReadSupportTotalsAsync()).ReturnsAsync(new SupportTotals(500, 40m));
        SupportTotals? written = null;
        _Files.Setup(f => f.WriteSupportTotals(It.IsAny<SupportTotals>()))
              .Callback<SupportTotals>(t => written = t);

        SupportTotalsFeature feature = CreateFeature();
        await feature.InitializeAsync(CancellationToken.None);
        await feature.HandleBitsAsync(Bits(100), CancellationToken.None);

        Assert.Equal(600, written!.BitsTotal);
        Assert.Equal(40m, written.DonationTotal);
    }

    [Fact]
    public async Task HandleBits_NonPositive_Ignored()
    {
        await CreateFeature().HandleBitsAsync(Bits(0), CancellationToken.None);

        _Files.Verify(f => f.WriteSupportTotals(It.IsAny<SupportTotals>()), Times.Never);
    }
}
