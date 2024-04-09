using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OBSWebsocketDotNet;
using SpekkieTwitchBot.Auth;
using SpekkieTwitchBot.FileHandling;
using SpekkieTwitchBot.FileHandling.General;
using SpekkieTwitchBot.FileHandling.Spotify;
using SpekkieTwitchBot.FileHandling.Timer;
using SpekkieTwitchBot.FileHandling.Twitch;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models;
using SpekkieTwitchBot.Models.Twitch;
using SpekkieTwitchBot.Spotify;
using SpekkieTwitchBot.Twitch.Client;
using SpekkieTwitchBot.Twitch.Commands;
using SpekkieTwitchBot.Twitch.Events;
using SpekkieTwitchBot.Twitch.Events.Handlers;
using SpekkieTwitchBot.Twitch.General;
using TwitchLib.EventSub.Websockets.Extensions;
using SpekkieTwitchBot.Web;

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
                services.AddSingleton<Logger>();
                services.AddSingleton<FileReader>();
                services.AddSingleton<FileWriter>();
                services.AddSingleton<FileSetup>();
                services.AddSingleton<SpotifyFileReader>();
                services.AddSingleton<SpotifyFileWriter>();
                services.AddSingleton<SpotifyFileSetup>();                
                services.AddSingleton<TwitchFileReader>();
                services.AddSingleton<TwitchFileWriter>();
                services.AddSingleton<TwitchFileSetup>();                
                services.AddSingleton<TimerFileReader>();
                services.AddSingleton<TimerFileWriter>();
                services.AddSingleton<TimerFileSetup>();                
                services.AddSingleton<GeneralFileReader>();
                services.AddSingleton<GeneralFileWriter>();
                services.AddSingleton<GeneralFileSetup>();

                services.AddSingleton<SubEventHandler>();
                services.AddSingleton<FollowEventHandler>();
                services.AddSingleton<ChannelPointHandler>();
                
                services.AddSingleton<AuthService>();
                services.AddSingleton<OBSWebsocket>();
                services.AddSingleton<IrcClient>();
                services.AddSingleton<SpotifyService>();
                services.AddSingleton<CustomTwitchClient>();
                services.AddSingleton<CustomPubsub>();
                services.AddSingleton<EventTimer>();
                services.AddSingleton<EventTimerService>();
                services.AddSingleton<SpotifyCommandHandler>();
                services.AddSingleton<TextCommandHandler>();
                services.AddSingleton<TimerCommandHandler>();
                services.AddSingleton<GeneralCommandHandler>();
                services.AddSingleton<JoinedChannelManager>();
                services.AddSingleton<IrcParser>();
                services.AddHostedService<SpotifyService>();
                services.AddHostedService<EventTimerService>();
                services.AddHostedService<ObsWebsocketService>();
                services.AddHostedService<TwitchWebsocketService>();
            });
}