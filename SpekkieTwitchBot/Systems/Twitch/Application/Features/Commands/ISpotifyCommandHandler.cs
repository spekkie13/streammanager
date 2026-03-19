namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public interface ISpotifyCommandHandler
{
    Task<string> HandleGetCurrentSongCommand(CancellationToken cancellationToken = default);
    Task<string> HandleGetCurrentPlaylistCommand(CancellationToken cancellationToken = default);
    Task<string> HandlePauseMusicCommand(CancellationToken cancellationToken = default);
    Task<string> HandleResumeMusicCommand(CancellationToken cancellationToken = default);
    Task<string> HandleNextSongCommand(CancellationToken cancellationToken = default);
    Task<string> HandlePrevSongCommand(CancellationToken cancellationToken = default);
    Task<string> HandleAddSongToQueueCommand(string songData, CancellationToken cancellationToken = default);
    Task<string> HandlePlaySpecificSongCommand(string song, string username, CancellationToken cancellationToken = default);
    Task<string> HandleGetQueueCommand(CancellationToken cancellationToken = default);
    Task<string> HandleSongRequestCommand(string input, string userId, string username, CancellationToken ct = default);
}
