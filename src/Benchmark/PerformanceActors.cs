using System.Collections.Generic;
using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Persistence;

namespace Benchmark
{
    public sealed class Init
    {
        public static readonly Init Instance = new Init();
        private Init() { }
    }

    public sealed class Finish
    {
        public static readonly Finish Instance = new Finish();
        private Finish() { }
    }

    public sealed class Done
    {
        public static readonly Done Instance = new Done();
        private Done() { }
    }

    public sealed class Finished
    {
        public readonly long State;

        public Finished(long state)
        {
            State = state;
        }
    }

    public sealed class Store
    {
        public readonly int Value;

        public Store(int value)
        {
            Value = value;
        }
    }

    public sealed class Stored
    {
        public readonly int Value;
        public readonly ImmutableList<string> Collection;

        public Stored(int value, ImmutableList<string> collection)
        {
            Value = value;
            Collection = collection;
        }
    }

    public class PerformanceTestActor : PersistentActor
    {
        private long state;

        public PerformanceTestActor(string persistenceId)
        {
            PersistenceId = persistenceId;
        }

        public sealed override string PersistenceId { get; }

        protected override bool ReceiveRecover(object message) => message.Match()
            .With<Stored>(s => state += s.Value)
            .WasHandled;

        protected override bool ReceiveCommand(object message) => message.Match()
            .With<Store>(store =>
            {
                Persist(new Stored(store.Value, ImmutableList.Create(store.Value.ToString())), s =>
                {
                    state += s.Value;
                });
            })
            .With<Init>(_ =>
            {
                var sender = Sender;
                Persist(new Stored(0, ImmutableList<string>.Empty), s =>
                {
                    state += s.Value;
                    sender.Tell(Done.Instance);
                });
            })
            .With<Finish>(_ => Sender.Tell(new Finished(state)))
            .WasHandled;
    }
}