using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Features;

namespace SpekkieTwitchBot.Systems.Twitch;

public class TwitchCommandHandler(ChannelPointsFeature channelPointsFeature)
{
    public string HandleCreateRedemptionCommand(string commandArgs)
    {
        return channelPointsFeature.CreateRedemption(commandArgs);
    }
}