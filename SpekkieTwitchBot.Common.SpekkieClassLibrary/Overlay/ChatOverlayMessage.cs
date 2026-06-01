namespace SpekkieClassLibrary.Overlay;

public class ChatOverlayMessage
{
    public string Id { get; set; } = "";
    public string User { get; set; } = "";      // display name (proper-cased)
    public string Login { get; set; } = "";     // lowercase login
    public string UserId { get; set; } = "";     // stable identity for coloring/dedup
    public string Text { get; set; } = "";       // plain text (fallback when Segments is empty)
    public List<ChatMessageSegment> Segments { get; set; } = []; // text/emote runs; empty if no emotes
    public string At { get; set; } = "";

    public string Color { get; set; } = "";      // authentic Twitch hex color, or "" (overlay falls back to a hash)
    public List<string> Badges { get; set; } = []; // ordered tokens, e.g. "broadcaster/1", "subscriber/12"

    public bool IsBroadcaster { get; set; }
    public bool IsMod { get; set; }
    public bool IsVip { get; set; }
    public bool IsSubscriber { get; set; }

    public bool IsAction { get; set; }            // /me message
    public bool IsFirstMessage { get; set; }
    public bool IsHighlighted { get; set; }       // channel-points "Highlight My Message"
    public int Bits { get; set; }

    public ChatReplyParent? ReplyParent { get; set; }
}

public class ChatReplyParent
{
    public string User { get; set; } = "";
    public string Text { get; set; } = "";
}
