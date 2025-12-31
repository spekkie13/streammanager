namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TwitchCommandHandler(ChannelPointsFeature channelPointsFeature)
{
    public string HandleCreateRedemptionCommand(string commandArgs)
    {
        return channelPointsFeature.CreateRedemption(commandArgs);
    }
}