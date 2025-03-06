using Moq;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using TwitchAuthService;
using TwitchAuthService.Handlers;

namespace SpekkieTwitchBot.Twitch.Tests;

public class FollowEventTests
{
    private readonly Mock<TwitchFileWriter> _mockTwitchFileWriter = new();
    private readonly Mock<TwitchFileReader> _mockTwitchFileReader = new();
    private readonly Mock<CustomTwitchHttpClient> _mockTwitchHttpClient = new();
    private readonly FollowEventHandler _handler;
    
    public FollowEventTests()
    {
        _mockTwitchFileReader = new Mock<TwitchFileReader>();
        _mockTwitchFileWriter = new Mock<TwitchFileWriter>();
        _mockTwitchHttpClient = new Mock<CustomTwitchHttpClient>();
        _handler = new FollowEventHandler(_mockTwitchFileWriter.Object, _mockTwitchFileReader.Object, _mockTwitchHttpClient.Object);
    }
    
    [Fact]
    public void HandleFollow_NewFollower_UpdatesFollowersFile()
    {
        
    }
}