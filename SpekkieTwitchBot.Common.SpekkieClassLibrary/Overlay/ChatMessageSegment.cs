namespace SpekkieClassLibrary.Overlay;

/// <summary>
/// A run of a chat message: either plain <c>text</c> or an <c>emote</c>. When a message has
/// segments the overlay renders these in order (emotes as images); otherwise it falls back to
/// <see cref="ChatOverlayMessage.Text"/>.
/// </summary>
public class ChatMessageSegment
{
    public string Type { get; set; } = "text"; // "text" | "emote"
    public string Text { get; set; } = "";      // text content, or the emote's name
    public string EmoteId { get; set; } = "";    // set when Type == "emote"
}
