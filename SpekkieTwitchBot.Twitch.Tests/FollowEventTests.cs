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

public class FollowEventHandlerTests
{
    private readonly Mock<TwitchFileWriter> _mockFileWriter;
    private readonly Mock<TwitchFileReader> _mockFileReader;
    private readonly Mock<CustomTwitchHttpClient> _mockHttpClient;
    private readonly FollowEventHandler _handler;

    public FollowEventHandlerTests()
    {
        Mock<FileReader> fileReaderMock = new Mock<FileReader>();
        Mock<FileWriter> fileWriterMock = new Mock<FileWriter>();
        
        _mockFileWriter = new Mock<TwitchFileWriter>(fileWriterMock.Object);
        _mockFileReader = new Mock<TwitchFileReader>(fileReaderMock.Object);
        Mock<GeneralFileWriter> mockGeneralFileWriter = new Mock<GeneralFileWriter>(fileWriterMock.Object);
        
        Mock<Logger> mockLogger = new Mock<Logger>(mockGeneralFileWriter.Object);

        Mock<TwitchAuthService.TwitchAuthService> mockTwitchAuthService = new Mock<TwitchAuthService.TwitchAuthService>(
            _mockFileReader.Object, 
            _mockFileWriter.Object, 
            mockLogger.Object);
        _mockHttpClient = new Mock<CustomTwitchHttpClient>(mockTwitchAuthService.Object);

        _handler = new FollowEventHandler(
            _mockFileReader.Object,
            _mockFileWriter.Object,
            _mockHttpClient.Object
        );
    }

    [Fact]
    public async Task HandleFollow_NewFollower_UpdatesFollowerFiles()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        var fakeMostRecentFollower = "OldFollower";

        var fakeResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"total\": 100, \"data\": [{ \"userName\": \"NewFollower\" }] }")
        };

        _mockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns(fakeMostRecentFollower);
        _mockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeResponse);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _mockFileWriter.Verify(w => w.WriteTotalFollowersFile(100), Times.Once);
        _mockFileWriter.Verify(w => w.WriteMostRecentFollowerFile("NewFollower"), Times.Once);
    }

    [Fact]
    public void HandleFollow_SameFollower_DoesNotUpdateFiles()
    {
        // Arrange
        var fakeFollower = "SameFollower";
        _mockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns(fakeFollower);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _handler.HandleFollow(null, eventArgs);

        // Assert
        _mockFileWriter.Verify(w => w.WriteTotalFollowersFile(It.IsAny<int>()), Times.Never);
        _mockFileWriter.Verify(w => w.WriteMostRecentFollowerFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleFollow_HttpRequestFails_DoesNotUpdateFiles()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _mockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");
        _mockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException());

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _mockFileWriter.Verify(w => w.WriteTotalFollowersFile(It.IsAny<int>()), Times.Never);
        _mockFileWriter.Verify(w => w.WriteMostRecentFollowerFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleFollow_MalformedJson_UsesDefaultValues()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _mockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");

        var fakeResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("INVALID_JSON")
        };

        _mockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeResponse);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _mockFileWriter.Verify(w => w.WriteTotalFollowersFile(0), Times.Once);
        _mockFileWriter.Verify(w => w.WriteMostRecentFollowerFile("N/A"), Times.Once);
    }

    [Fact]
    public async Task HandleFollow_EmptyFollowerList_WritesDefaultValues()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _mockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");

        var fakeResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"total\": 0, \"data\": [] }")
        };

        _mockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeResponse);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _mockFileWriter.Verify(w => w.WriteTotalFollowersFile(0), Times.Once);
        _mockFileWriter.Verify(w => w.WriteMostRecentFollowerFile("N/A"), Times.Once);
    }

    [Fact]
    public async Task HandleFollow_HttpRequestTimeout_DoesNotUpdateFiles()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _mockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");

        _mockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new TaskCanceledException()); // Simulate timeout

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _mockFileWriter.Verify(w => w.WriteTotalFollowersFile(It.IsAny<int>()), Times.Never);
        _mockFileWriter.Verify(w => w.WriteMostRecentFollowerFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleFollow_NullFollowerData_WritesDefaultValues()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _mockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");

        var fakeResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"total\": 100, \"data\": null }")
        };

        _mockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeResponse);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _mockFileWriter.Verify(w => w.WriteTotalFollowersFile(100), Times.Once);
        _mockFileWriter.Verify(w => w.WriteMostRecentFollowerFile("N/A"), Times.Once);
    }
}