namespace SpekkieClassLibrary.Events;

public interface IStreamEventBus
{
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler);
    Task PublishAsync<TEvent>(TEvent e, CancellationToken ct = default);
}
