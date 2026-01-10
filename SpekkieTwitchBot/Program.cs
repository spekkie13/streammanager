using EventTimerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
using SpekkieTwitchBot.Systems.Twitch.Abstractions.Auth;
using SpekkieTwitchBot.Systems.Twitch.Application.Features;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;
using SpekkieTwitchBot.Systems.Twitch.Application.Routing;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Auth;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Chat.Irc;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.Http;
using SpekkieTwitchBot.Systems.Twitch.Infrastructure.PubSub;
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
                    foreach (var inner in aex.Flatten().InnerExceptions)
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

        using var cts = new CancellationTokenSource();
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
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                    "/SpekkieTwitchBot/Settings/appsettings.json"
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
                // Features + Router
                // -----------------------
                services.AddSingleton<ChannelPointsFeature>();
                services.AddSingleton<FollowSubFeature>();
                services.AddSingleton<ChatCommandFeature>();
                services.AddSingleton<ChatMessageFeature>();
                services.AddSingleton<TwitchEventsFeature>();
                services.AddSingleton<TwitchEventRouter>();
                
                // -----------------------
                // Twitch core
                // -----------------------
                services.AddSingleton<CustomTwitchHttpClient>();
                services.AddSingleton<ICustomTwitchHttpClient>(sp => sp.GetRequiredService<CustomTwitchHttpClient>());

                services.AddSingleton<TwitchUserFile>();
                services.AddSingleton<TwitchGeneralFile>();

                services.AddSingleton<ITwitchAuthTokenProvider, FileBackedTwitchAuthTokenProvider>();
                
                // -----------------------
                // Command handlers
                // -----------------------
                services.AddSingleton<SpotifyCommandHandler>();
                services.AddSingleton<ObsCommandHandler>();
                services.AddSingleton<TextCommandHandler>();
                services.AddSingleton<TimerCommandHandler>();
                services.AddSingleton<TwitchCommandHandler>();
                services.AddSingleton<GeneralCommandHandler>();

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

                // Spotify core
                services.AddSingleton<SpotifyService>();
                services.AddHostedService<SpotifyHostedService>();
                
                // -----------------------
                // PubSub
                // -----------------------
                services.AddSingleton<PubSubWebSocketClient>();
                services.AddSingleton<PubSubReconnectPolicy>();
                services.AddSingleton<PubSubMessageBuilder>();
                services.AddSingleton<PubSubMessageParser>();

                services.AddSingleton<TwitchPubSubClient>();
                services.AddSingleton<ITwitchEvents>(sp => sp.GetRequiredService<TwitchPubSubClient>());

                // -----------------------
                // OBS / Timer
                // -----------------------
                services.AddSingleton<ObsWebSocket>();
                services.AddHostedService<ObsWebsocketService>();

                // Timer domain object (used by your timer hosted service)
                services.AddSingleton<EventTimer>();

                // EventTimerService is HostedService (single instance)
                services.AddSingleton<EventTimerService.EventTimerService>();
                services.AddHostedService(sp => sp.GetRequiredService<EventTimerService.EventTimerService>());

                // -----------------------
                // Hosted Services (Twitch)
                // -----------------------
                services.AddHostedService<TwitchHostedService>();
            });
    }
}
