using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OBSWebsocketDotNet;
using SpekkieTwitchBot.Models;
using SpekkieTwitchBot.Models.Twitch;
using SpekkieTwitchBot.Spotify;
using SpekkieTwitchBot.Twitch;
using SpekkieTwitchBot.Twitch.Commands;
using TwitchLib.EventSub.Websockets.Extensions;
using SpekkieTwitchBot.Web;
using TwitchLib.Client;
using TwitchLib.PubSub;

namespace SpekkieTwitchBot;
public static class Program
{
    static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(configure =>
            {
                configure.AddJsonFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/SpekkieTwitchBot/Settings/appsettings.json");
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                services.AddTwitchLibEventSubWebsockets();
                services.AddSingleton<OBSWebsocket>();
                services.AddSingleton<TwitchClient>();
                services.AddSingleton<TwitchPubSub>();
                services.AddSingleton<IrcClient>();
                services.AddSingleton<SpotifyService>();
                services.AddSingleton<EventTimer>();
                services.AddSingleton<EventTimerService>();
                services.AddSingleton<SpotifyCommandHandler>();
                services.AddSingleton<TextCommandHandler>();
                services.AddSingleton<TimerCommandHandler>();
                services.AddHostedService<SpotifyService>();
                services.AddHostedService<EventTimerService>();
                services.AddHostedService<ObsWebsocketService>();
                services.AddHostedService<TwitchChatService>();
                services.AddHostedService<TwitchWebsocketService>();
            });
}