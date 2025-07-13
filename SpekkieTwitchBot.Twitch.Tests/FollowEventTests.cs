using Moq;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using TwitchAuthService.Handlers;
using TwitchAuthService.Interfaces;
using TwitchLib.PubSub.Events;

namespace SpekkieTwitchBot.Twitch.Tests;

public class FollowEventHandlerTests
{
    private readonly Mock<ITwitchFileWriter> _MockFileWriter;
    private readonly Mock<ITwitchFileReader> _MockFileReader;
    private readonly Mock<ICustomTwitchHttpClient> _MockHttpClient;
    private readonly FollowEventHandler _Handler;

    public FollowEventHandlerTests()
    {
        _MockFileWriter = new Mock<ITwitchFileWriter>();
        _MockFileReader = new Mock<ITwitchFileReader>();
        _MockHttpClient = new Mock<ICustomTwitchHttpClient>();

        _Handler = new FollowEventHandler(_MockFileReader.Object, _MockFileWriter.Object, _MockHttpClient.Object);
    }

    [Fact]
    public async Task HandleFollow_NewFollower_UpdatesFollowerFiles()
    {
        // Arrange
        const string NewFollower = "NewFollower";
        const int FollowerCount = 100;

        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFileAsync()).ReturnsAsync("OldFollower");
        _MockHttpClient.Setup(h => h.GetFollowerCount()).ReturnsAsync(FollowerCount);

        OnFollowArgs eventArgs = new () { DisplayName = NewFollower };

        // Act
        await _Handler.ProcessFollowAsync(null, eventArgs);

        // Assert
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFileAsync(NewFollower), Times.Once);
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(FollowerCount), Times.Once);
    }

    [Fact]
    public async Task HandleFollow_FollowerCountRequestFails_DoesNotUpdateFiles()
    {
        const string NewFollower = "AnotherFollower";

        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFileAsync()).ReturnsAsync("OldFollower");
        _MockHttpClient.Setup(h => h.GetFollowerCount()).ThrowsAsync(new TimeoutException());

        OnFollowArgs eventArgs = new () { DisplayName = NewFollower };

        await _Handler.ProcessFollowAsync(null, eventArgs);

        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFileAsync(It.IsAny<string>()), Times.Never);
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(It.IsAny<int>()), Times.Never);
    }
    
    [Fact]
    public async Task HandleFollow_FileReadFails_DoesNotUpdateFiles()
    {
        // Arrange
        const string FakeFollower = "NewFollower";
        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFileAsync()).ThrowsAsync(new IOException("File read error"));

        OnFollowArgs eventArgs = new() { DisplayName = FakeFollower };

        // Act
        await _Handler.ProcessFollowAsync(null, eventArgs);

        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(It.IsAny<int>()), Times.Never);
        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleFollow_FileWriteFails_DoesNotUpdateTotalFollowers()
    {
        const string NewFollower = "NewFollower";

        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFileAsync()).ReturnsAsync("SomeoneElse");
        _MockHttpClient.Setup(h => h.GetFollowerCount()).ReturnsAsync(999);
        _MockFileWriter.Setup(w => w.WriteMostRecentFollowerFileAsync(NewFollower)).ThrowsAsync(new IOException("Write error"));

        OnFollowArgs eventArgs = new () { DisplayName = NewFollower };

        await _Handler.ProcessFollowAsync(null, eventArgs);

        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(It.IsAny<int>()), Times.Never);
    }
    
    [Fact]
    public async Task HandleFollow_RequestTimeout_DoesNotUpdateFiles()
    {
        const string Follower = "TimeoutGuy";

        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFileAsync()).ReturnsAsync("SomeoneElse");
        _MockHttpClient.Setup(h => h.GetFollowerCount()).ThrowsAsync(new TaskCanceledException());

        OnFollowArgs eventArgs = new () { DisplayName = Follower };

        await _Handler.ProcessFollowAsync(null, eventArgs);

        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFileAsync(It.IsAny<string>()), Times.Never);
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(It.IsAny<int>()), Times.Never);
    }
    
    [Fact]
    public async Task HandleFollow_SameFollower_DoesNotUpdateFiles()
    {
        const string SameFollower = "SamePerson";

        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFileAsync()).ReturnsAsync(SameFollower);

        OnFollowArgs eventArgs = new () { DisplayName = SameFollower };

        await _Handler.ProcessFollowAsync(null, eventArgs);

        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFileAsync(It.IsAny<string>()), Times.Never);
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(It.IsAny<int>()), Times.Never);
    }
    
    [Fact]
    public async Task HandleFollow_NullDisplayName_DoesNotUpdateFiles()
    {
        OnFollowArgs eventArgs = new () { DisplayName = null };

        await _Handler.ProcessFollowAsync(null, eventArgs);

        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFileAsync(It.IsAny<string>()), Times.Never);
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HandleFollow_NullMostRecentFollower_UpdatesFiles()
    {
        const string NewFollower = "FirstFollower";
        const int FollowerCount = 1;

        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFileAsync()).ReturnsAsync(string.Empty);
        _MockHttpClient.Setup(h => h.GetFollowerCount()).ReturnsAsync(FollowerCount);

        OnFollowArgs eventArgs = new () { DisplayName = NewFollower };

        await _Handler.ProcessFollowAsync(null, eventArgs);

        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFileAsync(NewFollower), Times.Once);
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(FollowerCount), Times.Once);
    }

    [Fact]
    public async Task HandleFollow_MaxFollowerCount_UpdatesFiles()
    {
        const string NewFollower = "PopularStreamer";
        const int MaxFollowerCount = int.MaxValue;

        _MockFileReader.Setup(r => r.ReadMostRecentFollowerFileAsync())
            .ReturnsAsync("SomeoneElse");

        _MockHttpClient.Setup(h => h.GetFollowerCount()).ReturnsAsync(MaxFollowerCount);

        OnFollowArgs eventArgs = new () { DisplayName = NewFollower };

        await _Handler.ProcessFollowAsync(null, eventArgs);

        _MockFileWriter.Verify(w => w.WriteMostRecentFollowerFileAsync(NewFollower), Times.Once);
        _MockFileWriter.Verify(w => w.WriteTotalFollowersFileAsync(MaxFollowerCount), Times.Once);
    }
}