using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;

// Placeholder — vervangen door B+ algoritme implementatie
internal sealed class MarathonTimeCalculatorStub : IMarathonTimeCalculator
{
    public TimeSpan CalculateForSub(SubHappened sub) => TimeSpan.Zero;
    public TimeSpan CalculateForBits(int bits) => TimeSpan.Zero;
    public TimeSpan CalculateForDonation(decimal euros) => TimeSpan.Zero;
}
