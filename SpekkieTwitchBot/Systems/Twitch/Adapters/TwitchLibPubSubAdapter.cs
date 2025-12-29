using SpekkieClassLibrary.Twitch.Pubsub.EventsArgs;
using SpekkieClassLibrary.Twitch.Pubsub.Types;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Models;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using TwitchAuthService.Events.Pubsub;
using TwitchLib.PubSub.Events;

namespace SpekkieTwitchBot.Systems.Twitch.TwitchLib;

public class TwitchLibPubSubAdapter : ITwitchEvents
{
    private readonly CustomPubsub _Pubsub;
    private readonly TwitchGeneralFile _Identity;
    private readonly TwitchUserFile _Tokens;
    private readonly Logger _Logger;
    
    public event Func<FollowHappened, CancellationToken, Task>? OnFollow;
    public event Func<SubHappened, CancellationToken, Task>? OnSub;
    public event Func<ChannelPointRedeemed, CancellationToken, Task>? OnChannelPointRedeemed;

    public TwitchLibPubSubAdapter(Logger logger, CustomPubsub pubsub, TwitchGeneralFile identity, TwitchUserFile tokens)
    {
        _Pubsub = pubsub;
        _Identity = identity;
        _Tokens = tokens;
        _Logger = logger;

        _Pubsub.OnFollow += HandleFollow;
        _Pubsub.OnChannelSubscription += HandleSubscription;
        _Pubsub.OnChannelPointsRewardRedeemed += HandleChannelPoints;

        _Pubsub.OnPubSubServiceClosed += (_, _) => _Pubsub.Connect();
        _Pubsub.OnPubSubServiceConnected += (_, _) =>
        {
            _Pubsub.OnFollow += HandleFollow;
            _Pubsub.OnChannelSubscription += HandleSubscription;
            _Pubsub.OnChannelPointsRewardRedeemed += HandleChannelPoints;

            _Pubsub.SendTopics(_Tokens.UserToken);
        };
    }
    
    
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _Pubsub.ListenToFollows(_Identity.ChannelId);
        _Pubsub.ListenToSubscriptions(_Identity.ChannelId);
        _Pubsub.ListenToChannelPoints(_Identity.ChannelId);
        
        _Pubsub.Connect();
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _Pubsub.Disconnect();
        return Task.CompletedTask;
    }

    private void HandleFollow(object? sender, OnFollowArgs e)
    {
        var model = new FollowHappened(
            UserId: e.UserId ?? "",
            UserName: e.DisplayName ?? e.Username ?? "",
            FollowedAt: DateTimeOffset.UtcNow
        );
        
        FireAndForget(RaiseAsync(OnFollow, model, CancellationToken.None));
    }

    private void HandleSubscription(object? sender, ChannelSubscriptionArgs e)
    {
        if (e.Subscription == null) return;
        var model = MapSubscription(e.Subscription);

        FireAndForget(RaiseAsync(OnSub, model, CancellationToken.None));
    }

    private void HandleChannelPoints(object? sender, ChannelPointsRewardRedeemedArgs e)
    {
        var redemption = e.RewardRedeemed?.Redemption;
        var reward = redemption?.Reward;

        if (redemption == null || reward == null) return;

        var model = new ChannelPointRedeemed(
            RedemptionId: redemption.Id ?? "",
            RewardId: reward.Id ?? "",
            RewardTitle: reward.Title ?? "",
            UserId: redemption.User?.Id ?? "",
            UserName: redemption.User?.DisplayName ?? redemption.User?.Login ?? "",
            UserInput: redemption.UserInput,
            RedeemedAt: redemption.RedeemedAt == default
                ? DateTimeOffset.UtcNow
                : new DateTimeOffset(redemption.RedeemedAt, TimeSpan.Zero)
        );
        
        FireAndForget(RaiseAsync(OnChannelPointRedeemed, model, CancellationToken.None));
    }
    
    private static SubHappened MapSubscription(ChannelSubscription s)
    {
        var kind = MapKind(s);

        bool isGift =
            kind is SubKind.Gift or SubKind.CommunityGift or SubKind.ContinuedGift or SubKind.PrimePaidUpgrade
            || s.IsGift;

        // Determine recipient & gifter
        string recipientId;
        string recipientName;
        string? gifterId = null;
        string? gifterName = null;

        if (isGift)
        {
            recipientId = s.RecipientId ?? "";
            recipientName = s.RecipientDisplayName ?? s.RecipientName ?? "";

            gifterId = s.UserId;
            gifterName = s.DisplayName ?? s.Username;
        }
        else
        {
            recipientId = s.UserId ?? "";
            recipientName = s.DisplayName ?? s.Username ?? "";
        }

        var tier = MapTier(s);

        bool isPrime = string.Equals(tier, "prime", StringComparison.OrdinalIgnoreCase);

        int? totalMonths = s.CumulativeMonths > 0 ? s.CumulativeMonths : null;

        int giftCount = 0;

        string? message = s.SubMessage?.ToString();
        if (string.IsNullOrWhiteSpace(message)) message = null;

        var ts = s.Time == default
            ? DateTimeOffset.UtcNow
            : new DateTimeOffset(DateTime.SpecifyKind(s.Time, DateTimeKind.Utc));

        return new SubHappened(
            Kind: kind,
            RecipientUserId: recipientId,
            RecipientUserName: recipientName,
            GifterUserId: gifterId,
            GifterUserName: gifterName,
            Tier: tier,
            IsPrime: isPrime,
            TotalMonths: totalMonths,
            GiftCount: giftCount,
            Message: message,
            Timestamp: ts
        );
    }
    
    private static string MapTier(ChannelSubscription s)
    {
        return s.SubscriptionPlan switch
        {
            SpekkieClassLibrary.Twitch.Pubsub.Enums.SubscriptionPlan.Prime => "prime",
            SpekkieClassLibrary.Twitch.Pubsub.Enums.SubscriptionPlan.Tier1 => "1000",
            SpekkieClassLibrary.Twitch.Pubsub.Enums.SubscriptionPlan.Tier2 => "2000",
            SpekkieClassLibrary.Twitch.Pubsub.Enums.SubscriptionPlan.Tier3 => "3000",
            _ => "1000"
        };
    }

    private static SubKind MapKind(ChannelSubscription s)
    {
        var ctx = (s.Context ?? "").Trim().ToLowerInvariant();

        if (ctx.Contains("primepaidupgrade")) return SubKind.PrimePaidUpgrade;
        if (ctx.Contains("giftpaidupgrade")) return SubKind.ContinuedGift;

        if (ctx.Contains("submysterygift")) return SubKind.CommunityGift;

        if (ctx.Contains("subgift") || ctx.Contains("anonsubgift")) return SubKind.Gift;

        if (ctx.Contains("resub")) return SubKind.Resub;

        return s.IsGift ? SubKind.Gift : SubKind.New;
    }

    private static async Task RaiseAsync<T>(
        Func<T, CancellationToken, Task>? evt,
        T payload,
        CancellationToken ct
    )
    {
        if (evt is null) return;

        foreach (var handler in evt.GetInvocationList().Cast<Func<T, CancellationToken, Task>>())
        {
            await handler(payload, ct);
        }
    }
    
    private void FireAndForget(Task task)
    {
        _ = task.ContinueWith(t =>
        {
            if (t.Exception != null)
                _Logger.LogError(t.Exception.Message);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}