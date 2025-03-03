using CommandService;
using CommandService.CommandHandlers;
using EventTimerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpekkieClassLibrary.Twitch.Auth;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Clash;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpekkieTwitchBot.General.FileHandling.Timer;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.OBS.OBSServiceNew;
using SpotifyAuthService;
using TwitchAuthService;
using TwitchAuthService.Events;
using TwitchAuthService.Events.Pubsub;
using TwitchAuthService.General;
using TwitchAuthService.Handlers;
using TwitchLib.EventSub.Websockets.Extensions;
using Websocket.Client;

namespace SpekkieTwitchBot;

public static class Program
{
    private static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(configure =>
            {
                configure.AddJsonFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                                      "/SpekkieTwitchBot/Settings/appsettings.json");
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                services.AddTwitchLibEventSubWebsockets();
                services.AddSingleton<HttpClient>();
                services.AddSingleton<WebsocketClient>(_ => new WebsocketClient(new Uri("ws://localhost:4455")));
                services.AddSingleton<Logger>();

                services.AddSingleton<FileSetup>();
                services.AddSingleton<FileReader>();
                services.AddSingleton<FileWriter>();
                services.AddSingleton<SpotifyFileSetup>();
                services.AddSingleton<SpotifyFileReader>();
                services.AddSingleton<SpotifyFileWriter>();
                services.AddSingleton<TwitchFileReader>();
                services.AddSingleton<TwitchFileWriter>();
                services.AddSingleton<TwitchFileSetup>();
                services.AddSingleton<TimerFileReader>();
                services.AddSingleton<TimerFileWriter>();
                services.AddSingleton<TimerFileSetup>();
//                services.AddSingleton<ClashFileReader>();
//                services.AddSingleton<ClashFileWriter>();
//                services.AddSingleton<ClashFileSetup>();
//                services.AddSingleton<ClashFileManager>();
                services.AddSingleton<GeneralFileReader>();
                services.AddSingleton<GeneralFileWriter>();
                services.AddSingleton<GeneralFileSetup>();
                services.AddSingleton<TwitchUserAuth>();

                services.AddSingleton<CustomClient>();
//                services.AddSingleton<CocHttpClient>();
//                services.AddSingleton<WarStatus>();

                services.AddSingleton<TwitchAuthService.TwitchAuthService>();
                services.AddSingleton<SpotifyAuthService.SpotifyAuthService>();
                services.AddSingleton<CustomTwitchHttpClient>();
                services.AddSingleton<CustomSpotifyHttpClient>();
                services.AddSingleton<SubEventHandler>();
                services.AddSingleton<FollowEventHandler>();
                services.AddSingleton<ChannelPointHandler>();
                services.AddSingleton<SpotifySearchService>();
                services.AddSingleton<ObsWebSocket>();
                services.AddSingleton<IrcClient>();
                services.AddSingleton<SpotifyService>();
                services.AddSingleton<CustomTwitchClient>();
                services.AddSingleton<CustomPubsub>();
                services.AddSingleton<EventTimer>();
                services.AddSingleton<EventTimerService.EventTimerService>();
                services.AddSingleton<SpotifyCommandHandler>();
                services.AddSingleton<ObsCommandHandler>();
                services.AddSingleton<TextCommandHandler>();
                services.AddSingleton<TimerCommandHandler>();

//                services.AddSingleton<ClashCommandHandler>();
                services.AddSingleton<TwitchCommandHandler>();
                services.AddSingleton<GeneralCommandHandler>();
                services.AddSingleton<JoinedChannelManager>();
//                services.AddSingleton<WarService>();
                services.AddSingleton<IrcParser>();
//                services.AddHostedService<WarService>();
                services.AddHostedService<SpotifyService>();
                services.AddHostedService<EventTimerService.EventTimerService>();
                services.AddHostedService<ObsWebsocketService>();
                services.AddHostedService<TwitchWebsocketService>();
            });
    }
}