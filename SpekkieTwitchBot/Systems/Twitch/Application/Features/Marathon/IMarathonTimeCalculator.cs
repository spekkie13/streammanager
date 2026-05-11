using SpekkieTwitchBot.Systems.Twitch.Models.Events;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;

public interface IMarathonTimeCalculator
{
    TimeSpan CalculateForSub(SubHappened sub);
    TimeSpan CalculateForBits(int bits);
    TimeSpan CalculateForDonation(decimal euros);
}
