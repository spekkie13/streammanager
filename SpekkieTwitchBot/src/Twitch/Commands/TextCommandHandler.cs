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

    public void HandleSpecsCommand()
    {
        _IrcClient.SendPublicChatMessage("Processor: AMD Ryzen 9 7950X3D | Motherboard: MSI MAG X670E TOMAHAWK WIFI | GPU: Gigabyte Radeon RX 7800 XT GAMING OC 16G Videokaart | SSD: Samsung 980 PRO 2TB | RAM: Corsair DDR5 2x32 GB 6000 | CPU Cooler: NZXT Kraken Z73 | CASE: NZXT H9 FLOW Black | PSU: Cooler Master MWE Gold 850");
    }
}