using CommandService.CommandHandlers;
using EventTimerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpekkieTwitchBot.General.FileHandling.Timer;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.OBS.OBSServiceNew;
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Features;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using SpekkieTwitchBot.Systems.Twitch.TwitchLib;
using SpotifyAuthService;
using SpotifyAuthService.General;
using TwitchAuthService.Events;
using TwitchAuthService.Events.Pubsub;
using TwitchAuthService.General;
using TwitchAuthService.Interfaces;
using TwitchLib.EventSub.Websockets.Extensions;
using WebsocketClient = Websocket.Client.WebsocketClient;

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
            .ConfigureServices(services =>
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
                services.AddSingleton<ITwitchFileReader, TwitchFileReader>();
                services.AddSingleton<TwitchFileReader>();
                services.AddSingleton<ITwitchFileWriter, TwitchFileWriter>();
                services.AddSingleton<TwitchFileWriter>();
                services.AddSingleton<TwitchFileSetup>();
                services.AddSingleton<TimerFileReader>();
                services.AddSingleton<TimerFileWriter>();
                services.AddSingleton<TimerFileSetup>();
                services.AddSingleton<GeneralFileReader>();
                services.AddSingleton<GeneralFileWriter>();
                services.AddSingleton<GeneralFileSetup>();

                services.AddSingleton<CustomClient>();

                services.AddSingleton<ICustomTwitchHttpClient, CustomTwitchHttpClient>();
                services.AddSingleton<CustomTwitchHttpClient>();
                services.AddSingleton<FollowSubFeature>();
                services.AddSingleton<CustomTwitchClient>();
                services.AddSingleton<CustomPubsub>();

                services.AddSingleton<TwitchUserFile>();
                services.AddSingleton<TwitchGeneralFile>();
                services.AddSingleton<TwitchEventRouter>();

                services.AddSingleton<SpotifyAuthService.Auth.SpotifyAuthService>();
                services.AddSingleton<CustomSpotifyHttpClient>();
                services.AddSingleton<SpotifySearchService>();
                services.AddSingleton<SpotifyService>();
                services.AddSingleton<CustomWebSocketClient>();
                services.AddSingleton<System.Net.WebSockets.ClientWebSocket>();
                services.AddSingleton<ObsWebSocket>();
                services.AddSingleton<EventTimer>();
                services.AddSingleton<EventTimerService.EventTimerService>();
                services.AddSingleton<ITwitchChat, TwitchLibChatAdapter>();
                services.AddSingleton<ITwitchAuthTokenProvider, FileBackedTwitchAuthTokenProvider>();
                services.AddSingleton<ITwitchEvents, TwitchLibPubSubAdapter>();
                
                services.AddSingleton<SpotifyCommandHandler>();
                services.AddSingleton<ObsCommandHandler>();
                services.AddSingleton<TextCommandHandler>();
                services.AddSingleton<TimerCommandHandler>();
                services.AddSingleton<TwitchCommandHandler>();
                services.AddSingleton<GeneralCommandHandler>();
                
                services.AddSingleton<JoinedChannelManager>();
                services.AddSingleton<IrcParser>();
                
                services.AddSingleton<TwitchLibPubSubAdapter>();
                services.AddSingleton<ChannelPointsFeature>();
                services.AddSingleton<FollowSubFeature>();
                services.AddSingleton<ChatCommandFeature>();
                services.AddSingleton<ChatMessageFeature>();
                services.AddSingleton<TwitchEventsFeature>();
                
                services.AddHostedService<SpotifyService>();
                services.AddHostedService<EventTimerService.EventTimerService>();
                services.AddHostedService<ObsWebsocketService>();
                services.AddHostedService<TwitchHostedService>();
            });
    }
}