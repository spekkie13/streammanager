namespace SpekkieTwitchBot.Models.Spotify.Internal;

internal static class Ensure
{
    public static void ArgumentNotNull(object value, string name)
    {
        if (value != null)
        {
            return;
        }

        throw new ArgumentNullException(name);
    }

    public static void ArgumentNotNullOrEmptyString(string value, string name)
    {
        if (!string.IsNullOrEmpty(value))
        {
            return;
        }

        throw new ArgumentException("String is empty or null", name);
    }

    public static void ArgumentNotNullOrEmptyList<T>(IEnumerable<T> value, string name)
    {
        if (value != null && value.Any())
        {
            return;
        }

        throw new ArgumentException("List is empty or null", name);
    }
}