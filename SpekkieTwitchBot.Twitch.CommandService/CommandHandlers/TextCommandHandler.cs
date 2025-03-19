namespace CommandService.CommandHandlers;

public class TextCommandHandler
{
    public static string HandleGetYouTubeCommand()
    {
        return "Checkout my YouTube Channel: https://youtube.com/@spekkieclashes";
    }

    public static string HandleGetDiscordCommand()
    {
        return 
            "Wanna connect off-stream? Join my discord server: https://discord.gg/8Ez2dZNxeV";
    }

    public static string HandleGetTwitterCommand()
    {
        return "Checkout my Twitter: https://twitter.com/CSpekkie";
    }

    public static string HandleGetCocTagCommand()
    {
        return "My in-game tag is #QYCCULVY";
    }

    public static string HandleLurkCommand(string username)
    {
        return $"Enjoy your lurk! {username}";
    }

    public static string HandleHelloCommand()
    {
        return "Hello World";
    }
}