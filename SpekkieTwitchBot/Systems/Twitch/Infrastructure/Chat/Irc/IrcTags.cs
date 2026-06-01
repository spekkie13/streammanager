using System.Text;

namespace SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat.Irc;

public static class IrcTags
{
    /// <summary>
    /// Decodes an IRCv3 escaped tag value (\s → space, \: → ;, \\ → \, \r, \n).
    /// Free-text tags such as reply-parent-msg-body arrive escaped this way.
    /// </summary>
    public static string Unescape(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('\\'))
            return value;

        StringBuilder sb = new(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\')
            {
                sb.Append(value[i]);
                continue;
            }

            if (i + 1 >= value.Length) break; // trailing lone backslash is dropped
            char next = value[++i];
            sb.Append(next switch
            {
                's' => ' ',
                ':' => ';',
                'r' => '\r',
                'n' => '\n',
                '\\' => '\\',
                _ => next
            });
        }
        return sb.ToString();
    }
}
