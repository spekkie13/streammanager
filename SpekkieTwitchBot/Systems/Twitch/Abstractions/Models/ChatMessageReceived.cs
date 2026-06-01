namespace SpekkieTwitchBot.Systems.Twitch.Abstractions.Models;

public sealed record ChatMessageReceived(
    string MessageId,
    string UserId,
    string Username,
    string Text,
    string? CustomRewardId)
{
    /// <summary>Lowercase IRC login (the prefix name); stable key. <see cref="Username"/> is the display name.</summary>
    public string Login { get; init; } = "";

    /// <summary>The user's Twitch name color as a hex string (e.g. "#1E90FF"), or "" if unset.</summary>
    public string Color { get; init; } = "";

    /// <summary>Raw IRC badges tag, e.g. "broadcaster/1,subscriber/12".</summary>
    public string Badges { get; init; } = "";

    /// <summary>Raw IRC emotes tag, e.g. "25:0-4,12-16/1902:6-10" (id:start-end by codepoint).</summary>
    public string Emotes { get; init; } = "";

    /// <summary>Total bits cheered in this message (0 if none).</summary>
    public int Bits { get; init; }

    /// <summary>True for a user's first-ever message in the channel.</summary>
    public bool IsFirstMessage { get; init; }

    /// <summary>True for a channel-points "Highlight My Message".</summary>
    public bool IsHighlighted { get; init; }

    /// <summary>Display name of the message this one replies to ("" if not a reply).</summary>
    public string ReplyParentDisplayName { get; init; } = "";

    /// <summary>Body of the message this one replies to ("" if not a reply).</summary>
    public string ReplyParentBody { get; init; } = "";
}
