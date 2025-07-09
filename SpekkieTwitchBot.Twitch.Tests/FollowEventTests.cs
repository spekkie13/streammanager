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
    private readonly Mock<TwitchFileWriter> _MockFileWriter;
    private readonly Mock<TwitchFileReader> _MockFileReader;
    private readonly Mock<CustomTwitchHttpClient> _MockHttpClient;
    private readonly FollowEventHandler _Handler;

    public FollowEventHandlerTests()
    {
        Mock<FileReader> fileReaderMock = new Mock<FileReader>();
        Mock<FileWriter> fileWriterMock = new Mock<FileWriter>();
        
        _MockFileWriter = new Mock<TwitchFileWriter>(fileWriterMock.Object);
        _MockFileReader = new Mock<TwitchFileReader>(fileReaderMock.Object);
        Mock<GeneralFileWriter> mockGeneralFileWriter = new Mock<GeneralFileWriter>(fileWriterMock.Object);
        
        Mock<Logger> mockLogger = new Mock<Logger>(mockGeneralFileWriter.Object);

        Mock<TwitchAuthService.TwitchAuthService> mockTwitchAuthService = new Mock<TwitchAuthService.TwitchAuthService>(
            _MockFileReader.Object, 
            _MockFileWriter.Object, 
            mockLogger.Object);
        _MockHttpClient = new Mock<CustomTwitchHttpClient>(mockTwitchAuthService.Object);

        _Handler = new FollowEventHandler(
            _MockFileReader.Object,
            _MockFileWriter.Object,
            _MockHttpClient.Object
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

        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns(fakeMostRecentFollower);
        _MockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeResponse);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _Handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFile(100), Times.Once);
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFile("NewFollower"), Times.Once);
    }

    [Fact]
    public void HandleFollow_SameFollower_DoesNotUpdateFiles()
    {
        // Arrange
        var fakeFollower = "SameFollower";
        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns(fakeFollower);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _Handler.HandleFollow(null, eventArgs);

        // Assert
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFile(It.IsAny<int>()), Times.Never);
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleFollow_HttpRequestFails_DoesNotUpdateFiles()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");
        _MockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ThrowsAsync(new HttpRequestException());

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _Handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFile(It.IsAny<int>()), Times.Never);
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleFollow_MalformedJson_UsesDefaultValues()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");

        var fakeResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("INVALID_JSON")
        };

        _MockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeResponse);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _Handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFile(0), Times.Once);
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFile("N/A"), Times.Once);
    }

    [Fact]
    public async Task HandleFollow_EmptyFollowerList_WritesDefaultValues()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");

        var fakeResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"total\": 0, \"data\": [] }")
        };

        _MockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeResponse);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _Handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFile(0), Times.Once);
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFile("N/A"), Times.Once);
    }

    [Fact]
    public async Task HandleFollow_HttpRequestTimeout_DoesNotUpdateFiles()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");

        _MockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>()))
            .ThrowsAsync(new TaskCanceledException()); // Simulate timeout

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _Handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFile(It.IsAny<int>()), Times.Never);
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleFollow_NullFollowerData_WritesDefaultValues()
    {
        // Arrange
        var fakeFollower = "NewFollower";
        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFile()).Returns("OldFollower");

        var fakeResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{ \"total\": 100, \"data\": null }")
        };

        _MockHttpClient.Setup(c => c.GetAsync(It.IsAny<string>())).ReturnsAsync(fakeResponse);

        var eventArgs = new OnFollowArgs { DisplayName = fakeFollower };

        // Act
        _Handler.HandleFollow(null, eventArgs);
        await Task.Delay(100);

        // Assert
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFile(100), Times.Once);
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFile("N/A"), Times.Once);
    }
}