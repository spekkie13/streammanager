namespace SpotifyAuthService;

public interface ISpotifyService
{
    Task<string> GetNowPlayingAsync(CancellationToken ct = default);
    Task<string> GetCurrentlyPlayingPlaylistAsync(CancellationToken ct = default);
    Task<string> GetQueueAsync(CancellationToken ct = default);
    Task<bool> PlaySpecificSongAsync(string song, CancellationToken ct = default);
    Task<bool> PausePlayerAsync(CancellationToken ct = default);
    Task<bool> ResumePlayerAsync(CancellationToken ct = default);
    Task<bool> SkipNextSongAsync(CancellationToken ct = default);
    Task<bool> SkipPrevSongAsync(CancellationToken ct = default);
    Task<string> AddSongToQueueAsync(string songUri, CancellationToken ct = default);
}
