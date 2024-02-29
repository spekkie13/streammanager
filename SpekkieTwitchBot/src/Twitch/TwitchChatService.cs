using Microsoft.Extensions.Hosting;
using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Models;
using SpekkieTwitchBot.Spotify;

namespace SpekkieTwitchBot.Twitch;

public class TwitchChatService : BackgroundService
{
    private readonly IrcClient _IrcClient;
    private readonly SpotifyService _SpotifyService;
    private readonly EventTimerService _EventTimerService;
    private static Dictionary<string, Action> _CommandHandlers = new ();
    private static string _BroadcasterName = "";

    public TwitchChatService(IrcClient ircClient, SpotifyService spotifyService, EventTimerService eventTimer)
    {
        _IrcClient = ircClient;
        _SpotifyService = spotifyService;
        _EventTimerService = eventTimer;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string twitchMessage = _IrcClient.ReadMessage();
            Console.WriteLine(twitchMessage);

            if (twitchMessage.Contains('!'))
                HandleCommand(twitchMessage);

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
    
    private void HandleCommand(string message)
    {
        int indexParseSign = message.IndexOf("!", StringComparison.Ordinal);
        string username = message[1..indexParseSign];

        indexParseSign = message.IndexOf(" :", StringComparison.Ordinal);
        message = message[(indexParseSign + 2)..];
        string command = message.Split(" ").First();

        if (!command.StartsWith("!")) return;

        _CommandHandlers = new Dictionary<string, Action>
        {
            { "!hello", HandleHelloCommand },
            { "!commands", HandleCommandsCommand },
            { "!twitter", HandleGetTwitterCommand },
            { "!youtube", HandleGetYouTubeCommand },
            { "!discord", HandleGetDiscordCommand },
            { "!exitbot", () => HandleExitBotCommand(username) },
            { "!pausetimer", HandlePauseTimerCommand },
            { "!starttimer", HandleStartTimerCommand },
            { "!song", HandleGetCurrentSongCommand },
            { "!playlist", HandleGetCurrentPlaylistCommand },
            { "!pausemusic", HandlePauseMusicCommand },
            { "!resumemusic", HandleResumeMusicCommand },
            { "!next", HandleNextSongCommand },
            { "!prev", HandlePrevSongCommand },
            { "!queue", HandleGetQueueCommand },
            { "!addsong", () => HandleAddSongToQueueCommand(message.Split(' ')[1]) },
            { "!playsong", () => HandlePlaySpecificSongCommand(message.Split(' ')[1], username) },
            { "!addtime", () => HandleAddTimeToTimerCommand(message.Split(' ')[1]) },
            { "!settime", () => HandleSetTimeOnTimerCommand(message.Split(' ')[1]) },
            { "!lurk", () => HandleLurkCommand(username) },
        };

        if (_CommandHandlers.TryGetValue(command, out Action? handler))
            handler.Invoke();
        else
            HandleUnknownCommand();
    }

    private void HandleCommandsCommand()
    {
        string commands = "";
        foreach (string command in _CommandHandlers.Keys)
        {
            if (command != _CommandHandlers.Keys.Last())
                commands += $"{command}, ";
            else
                commands += $"{command}";
        }
        _IrcClient.SendPublicChatMessage($"The following commands are available on this channel: {commands}");
    }

    private void HandleLurkCommand(string username)
    {
        _IrcClient.SendPublicChatMessage($"Enjoy your lurk {username}!");
    }

    private void HandleHelloCommand()
    {
        _IrcClient.SendPublicChatMessage("Hello World!");
    }

    private void HandleUnknownCommand()
    {
        _IrcClient.SendPublicChatMessage("Unknown command");
    }

    private void HandleExitBotCommand(string username)
    {
        if (!username.Equals(_BroadcasterName)) return;
        _IrcClient.SendPublicChatMessage("Bye! Have a beautiful time!");
        Environment.Exit(0);
    }

    private void HandlePauseTimerCommand()
    {
        _EventTimerService.StopAsync(default);
        _IrcClient.SendPublicChatMessage($"Pausing timer at: {_EventTimerService.GetRemainingTime()}");
    }

    private void HandleStartTimerCommand()
    {
        _EventTimerService.StartAsync(CancellationToken.None);
        _IrcClient.SendPublicChatMessage($"Resuming timer at: {_EventTimerService.GetRemainingTime()}");
    }

    private void HandleGetCurrentSongCommand()
    {
        string currentSong = _SpotifyService.GetNowPlaying();
        _IrcClient.SendPublicChatMessage($"The current song is {currentSong}");
    }

    private void HandleGetCurrentPlaylistCommand()
    {
        var currentPlaylistUrl = _SpotifyService.GetCurrentlyPlayingPlaylist();
        _IrcClient.SendPublicChatMessage($"The current playlist is {currentPlaylistUrl}");
    }

    private void HandlePauseMusicCommand()
    {
        bool success = _SpotifyService.PausePlayer().Result;
        string message = success ? "Player paused..." : "Player not paused due to an error...";
        _IrcClient.SendPublicChatMessage(message);
    }

    private void HandleResumeMusicCommand()
    {
        bool success = _SpotifyService.ResumePlayer().Result;
        string message = success ? "Player resumed..." : "Player not resumed due to an error...";
        _IrcClient.SendPublicChatMessage(message);
    }

    private void HandleNextSongCommand()
    {
        bool success = _SpotifyService.SkipNextSong().Result;
        string currentSong = _SpotifyService.GetNowPlaying();
        FileHandler.WriteSongFile(currentSong);

        _IrcClient.SendPublicChatMessage(success
            ? "Skipped to the next song..."
            : "Failed to skip to the next song...");
    }

    private void HandlePrevSongCommand()
    {
        bool success = _SpotifyService.SkipPrevSong().Result;
        string currentSong = _SpotifyService.GetNowPlaying();
        FileHandler.WriteSongFile(currentSong);

        _IrcClient.SendPublicChatMessage(success
            ? "Skipped to the previous song..."
            : "Failed to skip to the previous song...");
    }

    private void HandleAddSongToQueueCommand(string song)
    {
        bool success = _SpotifyService.AddSongToQueue(song).Result;

        string message = success ? "Added song to the queue..." : "Could not add song to the queue...";
        _IrcClient.SendPublicChatMessage(message);
    }

    private void HandlePlaySpecificSongCommand(string song, string username)
    {
        if (username != _BroadcasterName) return;
            bool success = _SpotifyService.PlaySpecificSong(song).Result;
        _IrcClient.SendPublicChatMessage(success ? $"started song: {song}" : $"failed to start song: {song}");
    }

    private void HandleGetQueueCommand()
    {
        string queue = _SpotifyService.GetQueue();
    
        _IrcClient.SendPublicChatMessage($"current queue: {queue}");
    }

    private void HandleAddTimeToTimerCommand(string timeToAdd)
    {
        TimeSpan InitialRemainingTime = _EventTimerService.GetRemainingTime();
        switch (timeToAdd)
        {
            case not null when timeToAdd.ToLower().Contains('s'):
                int duration = Convert.ToInt32(timeToAdd.Split('s')[0]);
                TimeSpan time = InitialRemainingTime + TimeSpan.FromSeconds(duration);
                _EventTimerService.SetRemainingTime(time);
                _IrcClient.SendPublicChatMessage($"added {duration} seconds to the timer");
                break;
            case not null when timeToAdd.ToLower().Contains('m'):
                duration = Convert.ToInt32(timeToAdd.Split('m')[0]);
                time = InitialRemainingTime + TimeSpan.FromMinutes(duration);
                _EventTimerService.SetRemainingTime(time);
                _IrcClient.SendPublicChatMessage($"added {duration} minutes to the timer");
                break;
            case not null when timeToAdd.ToLower().Contains('h'):
                duration = Convert.ToInt32(timeToAdd.Split('h')[0]);
                time = InitialRemainingTime + TimeSpan.FromHours(duration);
                _EventTimerService.SetRemainingTime(time);
                _IrcClient.SendPublicChatMessage($"added {duration} hours to the timer");
                break;
        }
    }

    private void HandleSetTimeOnTimerCommand(string time)
    {
        string[] parts = time.Split(":");
        TimeSpan newTime = new TimeSpan(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]), Convert.ToInt32(parts[2]));
        _IrcClient.SendPublicChatMessage($"Set timer to {parts[0].PadLeft(2, '0')}:{parts[1].PadLeft(2, '0')}:{parts[2].PadLeft(2, '0')}");
        _EventTimerService.SetRemainingTime(newTime);
    }

    private void HandleGetYouTubeCommand()
    {
        _IrcClient.SendPublicChatMessage("Checkout my YouTube Channel: https://youtube.com/@spekkieclashes");
    }

    private void HandleGetDiscordCommand()
    {
        _IrcClient.SendPublicChatMessage("Wanna connect off-stream? Join my discord server: https://discord.gg/8Ez2dZNxeV");
    }

    private void HandleGetTwitterCommand()
    {
        _IrcClient.SendPublicChatMessage("Checkout my Twitter: https://twitter.com/CSpekkie");
    }
}