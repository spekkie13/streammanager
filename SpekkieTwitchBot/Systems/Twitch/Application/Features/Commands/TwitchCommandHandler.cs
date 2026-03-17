namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class TwitchCommandHandler : ITwitchCommandHandler
{
    private readonly ChannelPointsFeature _ChannelPointsFeature;
    public TwitchCommandHandler(ChannelPointsFeature channelPointsFeature)
    {
        _ChannelPointsFeature = channelPointsFeature;
    }
    
    public async Task<string> HandleCreateRedemptionCommand(string commandArgs)
    {
        return await _ChannelPointsFeature.CreateRedemption(commandArgs);
    }
}