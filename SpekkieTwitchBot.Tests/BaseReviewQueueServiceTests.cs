using Moq;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.BaseReview;
using SpekkieTwitchBot.Systems.Twitch.Models.BaseReview;

namespace SpekkieTwitchBot.Tests;

public class BaseReviewQueueServiceTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"base-review-queue-{Guid.NewGuid():N}.json");

    private BaseReviewQueueService Create() =>
        new(new Mock<Logger>(MockBehavior.Loose, null!).Object, _path);

    private static BaseReviewEntry Entry(string user, bool isSub, string tier = "1000") =>
        new(
            UserId: user,
            UserName: user,
            Input: $"link-{user}",
            IsSubscriber: isSub,
            Tier: isSub ? tier : null,
            RedeemedAt: DateTimeOffset.UtcNow,
            RedemptionId: $"red-{user}",
            RewardId: "reward");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    // ── Ordering ───────────────────────────────────────────────

    [Fact]
    public async Task EnqueueAsync_NonSubscribers_PreservesFifoOrder()
    {
        BaseReviewQueueService queue = Create();

        await queue.EnqueueAsync(Entry("a", isSub: false));
        await queue.EnqueueAsync(Entry("b", isSub: false));
        await queue.EnqueueAsync(Entry("c", isSub: false));

        IReadOnlyList<BaseReviewEntry> snapshot = await queue.SnapshotAsync();
        Assert.Equal(new[] { "a", "b", "c" }, snapshot.Select(e => e.UserName));
    }

    [Fact]
    public async Task EnqueueAsync_Subscriber_PlacedAheadOfNonSubscribers()
    {
        BaseReviewQueueService queue = Create();

        await queue.EnqueueAsync(Entry("nonsub1", isSub: false));
        await queue.EnqueueAsync(Entry("nonsub2", isSub: false));
        int position = await queue.EnqueueAsync(Entry("sub1", isSub: true));

        IReadOnlyList<BaseReviewEntry> snapshot = await queue.SnapshotAsync();
        Assert.Equal(new[] { "sub1", "nonsub1", "nonsub2" }, snapshot.Select(e => e.UserName));
        Assert.Equal(1, position);
    }

    [Fact]
    public async Task EnqueueAsync_Subscriber_BehindEarlierSubscribers()
    {
        BaseReviewQueueService queue = Create();

        await queue.EnqueueAsync(Entry("sub1", isSub: true));
        await queue.EnqueueAsync(Entry("nonsub1", isSub: false));
        int position = await queue.EnqueueAsync(Entry("sub2", isSub: true));

        IReadOnlyList<BaseReviewEntry> snapshot = await queue.SnapshotAsync();
        Assert.Equal(new[] { "sub1", "sub2", "nonsub1" }, snapshot.Select(e => e.UserName));
        Assert.Equal(2, position);
    }

    // ── Dequeue ────────────────────────────────────────────────

    [Fact]
    public async Task DequeueAsync_ReturnsFrontAndRemovesIt()
    {
        BaseReviewQueueService queue = Create();
        await queue.EnqueueAsync(Entry("a", isSub: false));
        await queue.EnqueueAsync(Entry("b", isSub: false));

        BaseReviewEntry? first = await queue.DequeueAsync();

        Assert.NotNull(first);
        Assert.Equal("a", first!.UserName);
        IReadOnlyList<BaseReviewEntry> snapshot = await queue.SnapshotAsync();
        Assert.Equal(new[] { "b" }, snapshot.Select(e => e.UserName));
    }

    [Fact]
    public async Task DequeueAsync_EmptyQueue_ReturnsNull()
    {
        BaseReviewQueueService queue = Create();
        Assert.Null(await queue.DequeueAsync());
    }

    [Fact]
    public async Task ClearAsync_EmptiesQueue()
    {
        BaseReviewQueueService queue = Create();
        await queue.EnqueueAsync(Entry("a", isSub: false));

        await queue.ClearAsync();

        Assert.Empty(await queue.SnapshotAsync());
    }

    // ── Persistence ────────────────────────────────────────────

    [Fact]
    public async Task Queue_PersistsAcrossInstances()
    {
        BaseReviewQueueService queue = Create();
        await queue.EnqueueAsync(Entry("nonsub", isSub: false));
        await queue.EnqueueAsync(Entry("sub", isSub: true, tier: "2000"));

        // A fresh instance pointed at the same file reloads the persisted queue.
        BaseReviewQueueService reloaded = Create();
        IReadOnlyList<BaseReviewEntry> snapshot = await reloaded.SnapshotAsync();

        Assert.Equal(new[] { "sub", "nonsub" }, snapshot.Select(e => e.UserName));
        Assert.True(snapshot[0].IsSubscriber);
        Assert.Equal("2000", snapshot[0].Tier);
        Assert.Equal("link-nonsub", snapshot[1].Input);
    }
}
