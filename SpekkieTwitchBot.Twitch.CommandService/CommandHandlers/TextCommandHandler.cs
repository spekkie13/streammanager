namespace CommandService.CommandHandlers;

public class TextCommandHandler(IrcClient ircClient)
{
    public void HandleGetYouTubeCommand()
    {
        ircClient.SendPublicChatMessage("Checkout my YouTube Channel: https://youtube.com/@spekkieclashes");
    }

    public void HandleGetDiscordCommand()
    {
        ircClient.SendPublicChatMessage(
            "Wanna connect off-stream? Join my discord server: https://discord.gg/8Ez2dZNxeV");
    }

    public void HandleGetTwitterCommand()
    {
        ircClient.SendPublicChatMessage("Checkout my Twitter: https://twitter.com/CSpekkie");
    }

    public void HandleGetCocTagCommand()
    {
        ircClient.SendPublicChatMessage("My in-game tag is #QYCCULVY");
    }

    public void HandleLurkCommand(string username)
    {
        ircClient.SendPublicChatMessage($"Enjoy your lurk {username}!");
    }

    public void HandleHelloCommand()
    {
        ircClient.SendPublicChatMessage("Hello World!");
    }

    public void HandleSpecsCommand()
    {
        ircClient.SendPublicChatMessage(
            "Processor: AMD Ryzen 9 7950X3D | Motherboard: MSI MAG X670E TOMAHAWK WIFI | GPU: Gigabyte Radeon RX 7800 XT GAMING OC 16G Videokaart | SSD: Samsung 980 PRO 2TB | RAM: Corsair DDR5 2x32 GB 6000 | CPU Cooler: NZXT Kraken Z73 | CASE: NZXT H9 FLOW Black | PSU: Cooler Master MWE Gold 850");
    }
}