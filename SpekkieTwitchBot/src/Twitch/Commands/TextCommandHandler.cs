using SpekkieTwitchBot.Twitch.General;

namespace SpekkieTwitchBot.Twitch.Commands;

public class TextCommandHandler
{
    private readonly IrcClient _IrcClient;

    public TextCommandHandler(IrcClient ircClient)
    {
        _IrcClient = ircClient;
    }
    
    public void HandleGetYouTubeCommand()
    {
        _IrcClient.SendPublicChatMessage("Checkout my YouTube Channel: https://youtube.com/@spekkieclashes");
    }

    public void HandleGetDiscordCommand()
    {
        _IrcClient.SendPublicChatMessage("Wanna connect off-stream? Join my discord server: https://discord.gg/8Ez2dZNxeV");
    }

    public void HandleGetTwitterCommand()
    {
        _IrcClient.SendPublicChatMessage("Checkout my Twitter: https://twitter.com/CSpekkie");
    }

    public void HandleGetCocTagCommand()
    {
        _IrcClient.SendPublicChatMessage("My in-game tag is #QYCCULVY");
    }
    
    public void HandleLurkCommand(string username)
    {
        _IrcClient.SendPublicChatMessage($"Enjoy your lurk {username}!");
    }

    public void HandleHelloCommand()
    {
        _IrcClient.SendPublicChatMessage("Hello World!");
    }


}