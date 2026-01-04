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
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Adapters;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;
using SpekkieTwitchBot.Systems.Twitch.Application.Routing;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Auth;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Http;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using SpotifyAuthService;
using SpotifyAuthService.General;
using TwitchAuthService.Events;
using TwitchAuthService.General;
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
                configure.AddJsonFile(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                    "/SpekkieTwitchBot/Settings/appsettings.json"
                );
            })
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddTwitchLibEventSubWebsockets();

                // -----------------------
                // Core
                // -----------------------
                services.AddSingleton<HttpClient>();
                services.AddSingleton<WebsocketClient>(_ => new WebsocketClient(new Uri("ws://localhost:4455")));
                services.AddSingleton<Logger>();

                // -----------------------
                // Files
                // -----------------------
                services.AddSingleton<FileSetup>();
                services.AddSingleton<FileReader>();
                services.AddSingleton<FileWriter>();

                services.AddSingleton<SpotifyFileSetup>();
                services.AddSingleton<SpotifyFileReader>();
                services.AddSingleton<SpotifyFileWriter>();

                services.AddSingleton<TwitchFileSetup>();
                services.AddSingleton<TwitchFileReader>();
                services.AddSingleton<ITwitchFileReader>(sp => sp.GetRequiredService<TwitchFileReader>());

                services.AddSingleton<TwitchFileWriter>();
                services.AddSingleton<ITwitchFileWriter>(sp => sp.GetRequiredService<TwitchFileWriter>());

                services.AddSingleton<TimerFileSetup>();
                services.AddSingleton<TimerFileReader>();
                services.AddSingleton<TimerFileWriter>();

                services.AddSingleton<GeneralFileSetup>();
                services.AddSingleton<GeneralFileReader>();
                services.AddSingleton<GeneralFileWriter>();

                // -----------------------
                // Twitch core
                // -----------------------
                services.AddSingleton<CustomClient>();

                services.AddSingleton<CustomTwitchHttpClient>();
                services.AddSingleton<ICustomTwitchHttpClient>(sp => sp.GetRequiredService<CustomTwitchHttpClient>());

                services.AddSingleton<CustomTwitchClient>();

                services.AddSingleton<TwitchUserFile>();
                services.AddSingleton<TwitchGeneralFile>();
                services.AddSingleton<TwitchEventRouter>();

                services.AddSingleton<ITwitchAuthTokenProvider, FileBackedTwitchAuthTokenProvider>();

                // -----------------------
                // Adapters
                // -----------------------
                services.AddSingleton<ITwitchChat, TwitchLibChatAdapter>();

                // -----------------------
                // PubSub
                // -----------------------
                services.AddSingleton<PubSubWebSocketClient>();
                services.AddSingleton<PubSubMessageBuilder>();
                services.AddSingleton<PubSubMessageParser>();
                services.AddSingleton<PubSubReconnectPolicy>();

                services.AddSingleton<TwitchPubSubClient>();
                services.AddSingleton<ITwitchEvents>(sp => sp.GetRequiredService<TwitchPubSubClient>());

                // -----------------------
                // Spotify
                // -----------------------
                services.AddSingleton<SpotifyAuthService.Auth.SpotifyAuthService>();
                services.AddSingleton<CustomSpotifyHttpClient>();
                services.AddSingleton<SpotifyService>();
                services.AddSingleton<SpotifySearchService>();

                // -----------------------
                // OBS / Timer
                // -----------------------
                services.AddSingleton<ObsWebSocket>();
                services.AddSingleton<EventTimer>();
                services.AddSingleton<EventTimerService.EventTimerService>();

                // -----------------------
                // Command handlers
                // -----------------------
                services.AddSingleton<SpotifyCommandHandler>();
                services.AddSingleton<ObsCommandHandler>();
                services.AddSingleton<TextCommandHandler>();
                services.AddSingleton<TimerCommandHandler>();
                services.AddSingleton<TwitchCommandHandler>();
                services.AddSingleton<GeneralCommandHandler>();

                // Chat parsing helpers
                services.AddSingleton<JoinedChannelManager>();
                services.AddSingleton<IrcParser>();

                // -----------------------
                // Features
                // -----------------------
                services.AddSingleton<ChannelPointsFeature>();
                services.AddSingleton<FollowSubFeature>();
                services.AddSingleton<ChatCommandFeature>();
                services.AddSingleton<ChatMessageFeature>();
                services.AddSingleton<TwitchEventsFeature>();

                // -----------------------
                // Hosted Services
                // -----------------------
                services.AddHostedService(sp => sp.GetRequiredService<SpotifyService>());
                services.AddHostedService(sp => sp.GetRequiredService<EventTimerService.EventTimerService>());
                services.AddHostedService<ObsWebsocketService>();
                services.AddHostedService<TwitchHostedService>();
            });
    }
}
