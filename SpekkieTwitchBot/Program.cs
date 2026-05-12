using EventTimerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpekkieTwitchBot.General.FileHandling;
using SpekkieTwitchBot.General.FileHandling.Common;
using SpekkieTwitchBot.General.FileHandling.Common.Interface;
using SpekkieTwitchBot.General.FileHandling.General;
using SpekkieTwitchBot.General.FileHandling.Spotify;
using SpekkieTwitchBot.General.FileHandling.Timer;
using SpekkieTwitchBot.General.FileHandling.Twitch;
using SpekkieTwitchBot.General.FileHandling.Twitch.Interface;
using SpekkieTwitchBot.Systems.OBS;
using SpekkieTwitchBot.Systems.Twitch;
using SpekkieTwitchBot.Systems.Twitch.Abstractions;
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Marathon;
using SpekkieTwitchBot.ClashOfClans.StatsBot;
using SpekkieTwitchBot.General.FileHandling.Clash;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;
using SpekkieTwitchBot.Systems.Twitch.Application.Routing;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Auth;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat.Irc;
using SpekkieTwitchBot.Events;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Http;
using SpekkieTwitchBot.Systems.StreamStats;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.EventSub;
using SpekkieTwitchBot.Systems.StreamElements;
using SpekkieClassLibrary.Events;
using SpekkieTwitchBot.Systems.OBS.Websocket;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;
using SpekkieTwitchBot.Systems.Twitch.Models.Auth;
using SpotifyAuthService;
using WebsocketClient = Websocket.Client.WebsocketClient;

namespace SpekkieTwitchBot;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("[BOOT] Main started");

        IHost host;
        try
        {
            Console.WriteLine("[BOOT] About to Build()");
            host = CreateHostBuilder(args).Build();
            Console.WriteLine("[BOOT] Build() returned");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[BOOT] FATAL during host build:");
            Console.WriteLine(ex);
            return;
        }

        using (host)
        {
            host.Services.GetRequiredService<WarObsHandler>().Register();

            Console.WriteLine("[BOOT] Starting host...");

            try
            {
                await WithTimeout(host.StartAsync, TimeSpan.FromSeconds(10), "host.StartAsync");
                Console.WriteLine("[BOOT] Host started");

                await host.WaitForShutdownAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[BOOT] FATAL during host build:");
                Console.WriteLine(ex);

                if (ex is AggregateException aex)
                {
                    Console.WriteLine("[BOOT] AggregateException inner exceptions:");
                    foreach (Exception inner in aex.Flatten().InnerExceptions)
                    {
                        Console.WriteLine("---- INNER ----");
                        Console.WriteLine(inner);
                    }
                }
            }
        }
    }

    private static async Task WithTimeout(Func<CancellationToken, Task> action, TimeSpan timeout, string name)
    {
        Probe.Log($"WithTimeout ENTER: {name} timeout={timeout.TotalSeconds:0}s");

        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(timeout);

        // Start op background thread zodat sync-blokkades ook zichtbaar worden
        Task op = Task.Run(() => action(cts.Token), CancellationToken.None);

        Probe.Log($"WithTimeout STARTED (Task.Run): {name}");

        Task winner = await Task.WhenAny(op, Task.Delay(timeout)).ConfigureAwait(false);

        if (winner != op)
        {
            Probe.Log($"WithTimeout TIMEOUT: {name}");
            // Let op: op draait mogelijk nog door (non-cooperative). Dit is OK; je hebt nu wél diagnose.
            throw new TimeoutException($"{name} timed out after {timeout.TotalSeconds:0}s");
        }

        // propagate exceptions
        await op.ConfigureAwait(false);

        Probe.Log($"WithTimeout SUCCESS: {name}");
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseDefaultServiceProvider(options =>
            {
                options.ValidateScopes = true;
                options.ValidateOnBuild = true;
            })
            .ConfigureAppConfiguration(configure =>
            {
                configure.AddJsonFile(
                    Path.Combine(BotPaths.BaseDir, "Settings", "appsettings.json")
                );
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
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
                services.AddSingleton<ITextFileWriter>(sp => sp.GetRequiredService<FileWriter>());
                services.AddSingleton<IClashFileWriter>(sp => sp.GetRequiredService<ClashFileWriter>());

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
                services.AddSingleton<ITimerFileWriter>(sp => sp.GetRequiredService<TimerFileWriter>());

                services.AddSingleton<GeneralFileSetup>();
                services.AddSingleton<GeneralFileReader>();
                services.AddSingleton<GeneralFileWriter>();

                // -----------------------
                // StreamStats
                // -----------------------
                services.AddSingleton<StreamStatsClient>();

                // -----------------------
                // Features + Router
                // -----------------------
                services.AddSingleton<ChannelPointsFeature>();
                services.AddSingleton<FollowSubFeature>();
                services.AddSingleton<ChatCommandFeature>();
                services.AddSingleton<ChatMessageFeature>();
                services.AddSingleton<TimedMessagesFeature>();
                services.AddSingleton<IMarathonTimeCalculator, MarathonTimeCalculator>();
                services.AddSingleton<MarathonTimerFeature>();
                services.AddSingleton<TwitchEventRouter>();
                
                // -----------------------
                // Twitch core
                // -----------------------
                services.AddSingleton<CustomTwitchHttpClient>();
                services.AddSingleton<ICustomTwitchHttpClient>(sp => sp.GetRequiredService<CustomTwitchHttpClient>());
                services.AddSingleton<ITwitchChannelInfoClient>(sp => sp.GetRequiredService<CustomTwitchHttpClient>());

                services.AddSingleton<TwitchUserFile>();
                services.AddSingleton<TwitchGeneralFile>();

                services.AddSingleton<ITwitchAuthTokenProvider, FileBackedTwitchAuthTokenProvider>();
                
                // -----------------------
                // -----------------------
                // Event bus
                // -----------------------
                services.AddSingleton<StreamEventBus>();
                services.AddSingleton<IStreamEventBus>(sp => sp.GetRequiredService<StreamEventBus>());
                services.AddSingleton<WarObsHandler>();

                // -----------------------
                // Clash of Clans
                // -----------------------
                services.AddSingleton<ClashFileReader>();
                services.AddSingleton<ClashFileWriter>();
                services.AddSingleton<ClashFileManager>();
                services.AddSingleton<CocHttpClient>();
                services.AddSingleton<CcnHttpClient>();
                services.AddSingleton<WarStatus>();
                services.AddSingleton<WarService>();
                services.AddSingleton<IWarService>(sp => sp.GetRequiredService<WarService>());
                services.AddHostedService(sp => sp.GetRequiredService<WarService>());

                // -----------------------
                // Command handlers
                // -----------------------
                services.AddSingleton<SpotifyCommandHandler>();
                services.AddSingleton<ISpotifyCommandHandler>(sp => sp.GetRequiredService<SpotifyCommandHandler>());
                services.AddSingleton<ObsCommandHandler>();
                services.AddSingleton<IObsCommandHandler>(sp => sp.GetRequiredService<ObsCommandHandler>());
                services.AddSingleton<TextCommandHandler>();
                services.AddSingleton<ITextCommandHandler>(sp => sp.GetRequiredService<TextCommandHandler>());
                services.AddSingleton<TimerCommandHandler>();
                services.AddSingleton<ITimerCommandHandler>(sp => sp.GetRequiredService<TimerCommandHandler>());
                services.AddSingleton<TwitchCommandHandler>();
                services.AddSingleton<ITwitchCommandHandler>(sp => sp.GetRequiredService<TwitchCommandHandler>());
                services.AddSingleton<ClashCommandHandler>();
                services.AddSingleton<IClashCommandHandler>(sp => sp.GetRequiredService<ClashCommandHandler>());
                services.AddSingleton<GeneralCommandHandler>();
                services.AddSingleton<IGeneralCommandHandler>(sp => sp.GetRequiredService<GeneralCommandHandler>());
                services.AddSingleton<ICommandPermissionService, CommandPermissionService>();

                // -----------------------
                // Chat (IRC)
                // -----------------------
                services.AddSingleton<TwitchIrcWebSocketTransport>();
                services.AddSingleton<TwitchIrcChatClient>();
                services.AddSingleton<ITwitchChat>(sp => sp.GetRequiredService<TwitchIrcChatClient>());

                // -----------------------
                // Spotify
                // -----------------------
                services.AddSingleton<SpotifyAuthService.Auth.SpotifyAuthService>();

                services.AddSingleton<SpotifyAuthService.General.CustomSpotifyHttpClient>();
                services.AddSingleton<SpotifySearchService>();
                services.AddSingleton<ISpotifySearchService>(sp => sp.GetRequiredService<SpotifySearchService>());

                // Spotify core
                services.AddSingleton<SpotifyService>();
                services.AddSingleton<ISpotifyService>(sp => sp.GetRequiredService<SpotifyService>());
                services.AddHostedService<SpotifyHostedService>();
                
                // -----------------------
                // EventSub
                // -----------------------
                services.AddSingleton<EventSubWebSocketClient>();
                services.AddSingleton<TwitchEventSubClient>();
                services.AddSingleton<ITwitchEvents>(sp => sp.GetRequiredService<TwitchEventSubClient>());

                // -----------------------
                // OBS / Timer
                // -----------------------
                services.AddSingleton<ObsWebSocket>();
                services.AddSingleton<IObsWebSocket>(sp => sp.GetRequiredService<ObsWebSocket>());
                services.AddHostedService<ObsWebsocketService>();

                // Timer domain object (used by your timer hosted service)
                services.AddSingleton<EventTimer>();

                // EventTimerService is HostedService (single instance)
                services.AddSingleton<EventTimerService.EventTimerService>();
                services.AddSingleton<IEventTimerService>(sp => sp.GetRequiredService<EventTimerService.EventTimerService>());
                services.AddHostedService(sp => sp.GetRequiredService<EventTimerService.EventTimerService>());

                // -----------------------
                // StreamElements
                // -----------------------
                services.AddSingleton<StreamElementsSocketIoClient>();
                services.AddSingleton<StreamElementsClient>();
                services.AddHostedService<StreamElementsHostedService>();

                // -----------------------
                // Hosted Services (Twitch)
                // -----------------------
                services.AddHostedService<TwitchHostedService>();
            });
    }
}
