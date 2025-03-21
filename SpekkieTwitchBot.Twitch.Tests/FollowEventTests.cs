using System.Net;
using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using TwitchAuthService;
using TwitchAuthService.Handlers;
using TwitchLib.PubSub.Events;

namespace SpekkieTwitchBot.Twitch.Tests;

public class FollowEventTests
{
    private readonly Mock<TwitchFileWriter> _mockTwitchFileWriter;
    private readonly Mock<TwitchFileReader> _mockTwitchFileReader;
    private readonly Mock<CustomTwitchHttpClient> _mockTwitchHttpClient;
    private readonly FollowEventHandler _handler;
    
    public FollowEventTests()
    {
        Mock<FileWriter> fileWriterMock = new Mock<FileWriter>();
        Mock<FileReader> fileReaderMock = new Mock<FileReader>();
        Mock<GeneralFileWriter> generalFileWriterMock = new Mock<GeneralFileWriter>(fileWriterMock.Object);
        Mock<Logger> loggerMock = new Mock<Logger>(generalFileWriterMock.Object);

        _mockTwitchFileReader = new Mock<TwitchFileReader>(fileReaderMock.Object);
        _mockTwitchFileWriter = new Mock<TwitchFileWriter>(fileWriterMock.Object);
        
        Mock<TwitchAuthService.TwitchAuthService> twitchAuthServiceMock = new Mock<TwitchAuthService.TwitchAuthService>(_mockTwitchFileReader.Object, _mockTwitchFileWriter.Object, loggerMock.Object);
        _mockTwitchHttpClient = new Mock<CustomTwitchHttpClient>(twitchAuthServiceMock.Object, _mockTwitchFileWriter.Object);
        _handler = new FollowEventHandler(_mockTwitchFileReader.Object, _mockTwitchFileWriter.Object, _mockTwitchHttpClient.Object);
    }
    
    [Fact]
    public async Task HandleFollow_NewFollower_UpdatesFollowersFile()
    {
        const string FakeFollower = "NewFollower";
        const string FakeMostRecentFollower = "OldFollower";

        HttpResponseMessage response = new ()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"total\": 100, \"data\": [{ \"userName\": \"NewFollower\" }] }")
        };
        
        _mockTwitchFileReader.Setup(r => r.ReadMostRecentFollowerFile())
            .Returns(FakeMostRecentFollower);
        
        _mockTwitchHttpClient
            .Setup(c => c.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(response);

        OnFollowArgs eventArgs = new OnFollowArgs { DisplayName = FakeFollower };
        
        _handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);
        
        _mockTwitchFileWriter.Verify(w => w.WriteTotalFollowersFile(100), Times.Once);
        _mockTwitchFileWriter.Verify(w => w.WriteMostRecentFollowerFile("NewFollower"), Times.Once);
    }
}