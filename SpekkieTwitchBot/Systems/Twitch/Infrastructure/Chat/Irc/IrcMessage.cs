namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure;

public sealed class IrcMessage
{
    public Dictionary<string, string> Tags { get; }
    public string Prefix { get; }
    public string Username { get; }
    public string Command { get; }
    public string Channel { get; }
    public string Message { get; }

    private IrcMessage(
        Dictionary<string, string> tags,
        string prefix,
        string username,
        string command,
        string channel,
        string message)
    {
        Tags = tags;
        Prefix = prefix;
        Username = username;
        Command = command;
        Channel = channel;
        Message = message;
    }

    public static IrcMessage Parse(string raw)
    {
        // 1. Tags
        Dictionary<string, string> tags = new();
        if (raw.StartsWith("@"))
        {
            int space = raw.IndexOf(' ');
            string tagPart = raw[..space];
            raw = raw[(space + 1)..];

            foreach (string tag in tagPart[1..].Split(';'))
            {
                string[] kv = tag.Split('=', 2);
                tags[kv[0]] = kv.Length > 1 ? kv[1] : "";
            }
        }

        // 2. Prefix
        string prefix = "";
        if (raw.StartsWith(":"))
        {
            int space = raw.IndexOf(' ');
            prefix = raw[1..space];
            raw = raw[(space + 1)..];
        }

        // 3. Command + params
        string[] parts = raw.Split(" :", 2);
        string[] head = parts[0].Split(' ');

        string command = head[0];
        string channel = head.Length > 1 ? head[1] : "";
        string message = parts.Length > 1 ? parts[1] : "";

        string username = prefix.Contains('!')
            ? prefix.Split('!')[0]
            : prefix;

        return new IrcMessage(
            tags,
            prefix,
            username,
            command,
            channel,
            message
        );
    }
}