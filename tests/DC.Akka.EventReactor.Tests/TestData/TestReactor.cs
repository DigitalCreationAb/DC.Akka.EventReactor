using System.Collections.Concurrent;
using System.Collections.Immutable;
using Akka;
using Akka.Streams.Dsl;

namespace DC.Akka.EventReactor.Tests.TestData;

public class TestReactor(IImmutableList<Events.IEvent> events) : ITestReactor
{
    private readonly ConcurrentDictionary<string, int> _handledEvents = [];
    private readonly ConcurrentBag<string> _eventsToSkip = [];

    public string Name => GetType().Name;

    public ISetupEventReactor Configure(ISetupEventReactor config)
    {
        return ConfigureHandlers(config, _handledEvents);
    }

    public Source<IMessageWithAck, NotUsed> StartSource()
    {
        return Source.From(events.Where(x => !_eventsToSkip.Contains(x.EventId)))
            .Select(IMessageWithAck (x) => new EventWithAck(x, _eventsToSkip));
    }

    public static ISetupEventReactor ConfigureHandlers(
        ISetupEventReactor config,
        ConcurrentDictionary<string, int> handledEvents)
    {
        return config
            .On<Events.HandledEvent>(evnt => handledEvents
                .AddOrUpdate(evnt.EventId, _ => 1, (_, current) => current + 1))
            .On<Events.EventThatFails>(evnt => throw evnt.Exception);
    }

    private class EventWithAck(Events.IEvent evnt, ConcurrentBag<string> eventsToSkip) : IMessageWithAck
    {
        public object Message { get; } = evnt;

        public Task Ack()
        {
            eventsToSkip.Add(evnt.EventId);
            
            return Task.CompletedTask;
        }

        public Task Nack(Exception error)
        {
            eventsToSkip.Add(evnt.EventId);
            
            return Task.CompletedTask;
        }
    }

    public IImmutableDictionary<string, int> GetHandledEvents()
    {
        return _handledEvents.ToImmutableDictionary();
    }
}