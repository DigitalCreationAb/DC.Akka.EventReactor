namespace DC.Akka.EventReactor;

public interface IReactToEvent
{
    Task Handle(object evnt, CancellationToken cancellationToken);
}