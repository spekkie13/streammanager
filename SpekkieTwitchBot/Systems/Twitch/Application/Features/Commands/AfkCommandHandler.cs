using EventTimerService;
using SpekkieTwitchBot.Systems.Overlay;
using SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands.Interfaces;

namespace SpekkieTwitchBot.Systems.Twitch.Application.Features.Commands;

public class AfkCommandHandler(IEventTimerService eventTimerService, IOverlayAfkWriter overlayAfkWriter)
    : IAfkCommandHandler
{
    public async Task<string> HandleAfkCommand(CancellationToken ct)
    {
        // Pause the countdown, then flag the overlay. Support actions can still ADD time while paused.
        // !afk pauses the timer, so afk + timerRunning=false are written together in one go.
        eventTimerService.StopTimer();
        await overlayAfkWriter.SetAfkAsync(afk: true, timerRunning: false, ct);
        return $"Spekkie is now AFK — timer paused at {eventTimerService.GetRemainingTime()}";
    }

    public async Task<string> HandleBackCommand(CancellationToken ct)
    {
        // !back resumes the timer, so afk=false + timerRunning=true are written together in one go.
        eventTimerService.StartTimer();
        await overlayAfkWriter.SetAfkAsync(afk: false, timerRunning: true, ct);
        return $"Spekkie is back — timer resumed at {eventTimerService.GetRemainingTime()}";
    }
}
