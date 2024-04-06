using SpekkieTwitchBot.General;
using SpekkieTwitchBot.Spotify;
using System.Media;
using System.Runtime.InteropServices;

namespace SpekkieTwitchBot.Twitch.Commands;

public class SpotifyCommandHandler
{
    private readonly SpotifyService _SpotifyService;
    private readonly IrcClient _IrcClient;

    public SpotifyCommandHandler(SpotifyService spotifyService, IrcClient ircClient)
    {
        _SpotifyService = spotifyService;
        _IrcClient = ircClient;
    }
    
    public void HandleGetCurrentSongCommand()
    {
        string currentSong = _SpotifyService.GetNowPlaying();
        _IrcClient.SendPublicChatMessage($"The current song is {currentSong}");
    }

    public void HandleGetCurrentPlaylistCommand()
    {
        var currentPlaylistUrl = _SpotifyService.GetCurrentlyPlayingPlaylist();
        _IrcClient.SendPublicChatMessage($"The current playlist is {currentPlaylistUrl}");
    }

    public void HandlePauseMusicCommand()
    {
        bool success = _SpotifyService.PausePlayer().Result;
        string message = success ? "Player paused..." : "Player not paused due to an error...";
        _IrcClient.SendPublicChatMessage(message);
    }

    public void HandleResumeMusicCommand()
    {
        bool success = _SpotifyService.ResumePlayer().Result;
        string message = success ? "Player resumed..." : "Player not resumed due to an error...";
        _IrcClient.SendPublicChatMessage(message);
    }

    public void HandleNextSongCommand()
    {
        bool success = _SpotifyService.SkipNextSong().Result;
        string currentSong = _SpotifyService.GetNowPlaying();
        FileHandler.WriteSongFile(currentSong);

        _IrcClient.SendPublicChatMessage(success
            ? "Skipped to the next song..."
            : "Failed to skip to the next song...");
    }

    public void HandlePrevSongCommand()
    {
        bool success = _SpotifyService.SkipPrevSong().Result;
        string currentSong = _SpotifyService.GetNowPlaying();
        FileHandler.WriteSongFile(currentSong);

        _IrcClient.SendPublicChatMessage(success
            ? "Skipped to the previous song..."
            : "Failed to skip to the previous song...");
    }

    public bool HandleAddSongToQueueCommand(string song)
    {
        if (song.Contains("open.spotify.com"))
        {
            bool success = _SpotifyService.AddSongToQueue(song).Result;
            string message = success ? "Added song to the queue..." : "Could not add song to the queue...";
            _IrcClient.SendPublicChatMessage(message);
            return success;
        }
        _IrcClient.SendPublicChatMessage("Please provide a valid spotify link");
        return false;
    }

    public void HandlePlaySpecificSongCommand(string song, string username)
    {
        if (username.ToLower() != "spekkie1313") return;
            bool success = _SpotifyService.PlaySpecificSong(song).Result;
        _IrcClient.SendPublicChatMessage(success ? $"started song: {song}" : $"failed to start song: {song}");
    }

    public void HandleGetQueueCommand()
    {
        string queue = _SpotifyService.GetQueue();
    
        _IrcClient.SendPublicChatMessage($"current queue: {queue}");
    }

    public void PlaySound()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        const string Path = @"C:\Users\tomsp\OneDrive\Bureaublad\Muziek\Gerenderde Projecten\Future Bounce WIP.wav";
        SoundPlayer player = new SoundPlayer
        {
            SoundLocation = Path
        };

        try
        {
            player.Play();
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
        }
    }
}