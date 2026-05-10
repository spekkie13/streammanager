using System.Collections.Concurrent;
using SpekkieClassLibrary.Events;
using SpekkieTwitchBot.General.FileHandling;

namespace SpekkieTwitchBot.Events;

public class StreamEventBus(Logger logger) : IStreamEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
    {
        _handlers.GetOrAdd(typeof(TEvent), _ => []).Add(handler);
    }

    public async Task PublishAsync<TEvent>(TEvent e, CancellationToken ct = default)
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out List<Delegate>? handlers)) return;

        foreach (Delegate handler in handlers.ToList())
        {
            try
            {
                await ((Func<TEvent, CancellationToken, Task>)handler)(e, ct);
            }
            catch (Exception ex)
            {
                logger.LogError($"[EventBus] Handler for {typeof(TEvent).Name} threw: {ex}");
            }
        }
    }
}
