using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;

namespace Benchmark
{
    internal static class Program
    {
        // if you want to benchmark your persistent storage provides, paste the configuration in string below
        // by default we're checking against in-memory journal
        private static readonly Config Config = ConfigurationFactory.ParseString(@"
            akka.actor {
                serializers {
                    hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                }
                serialization-bindings {
                    ""System.Object"" = hyperion
                }
            }
            akka.test.single-expect-default = 3s
            akka.persistence {
                publish-plugin-commands = on
                journal {
                    plugin = ""akka.persistence.journal.mongodb""
                    mongodb {
                        class = ""Akka.Persistence.MongoDb.Journal.MongoDbJournal, Akka.Persistence.MongoDb""
                        connection-string = ""mongodb://localhost:27017/akkanet""
                        auto-initialize = on
                        collection = ""EventJournal""
                    }
                }
                query {
                    mongodb {
                        class = ""Akka.Persistence.MongoDb.Query.MongoDbReadJournalProvider, Akka.Persistence.MongoDb""
                        refresh-interval = 1s
                    }
                }
            }");

        private const int ActorCount = 1;
        private const int MessagesPerActor = 1;

        private static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("persistent-benchmark", Config.WithFallback(ConfigurationFactory.Default())))
            {
                Console.WriteLine("Performance benchmark starting...");

                var stopwatch = new Stopwatch();

                var actors = new IActorRef[ActorCount];
                for (var i = 0; i < ActorCount; i++)
                {
                    var pid = "a-" + i;
                    actors[i] = system.ActorOf(Props.Create(() => new PerformanceTestActor(pid)));
                }

                stopwatch.Start();

                Task.WaitAll(actors.Select(a => a.Ask<Done>(Init.Instance)).Cast<Task>().ToArray());

                stopwatch.Stop();

                Console.WriteLine($"Initialized {ActorCount} eventsourced actors in {stopwatch.ElapsedMilliseconds / 1000.0} sec...");

                stopwatch.Start();

                for (var i = 0; i < MessagesPerActor; i++)
                {
                    for (var j = 0; j < ActorCount; j++)
                    {
                        actors[j].Tell(new Store(1));
                    }
                }

                var finished = new Task[ActorCount];
                for (var i = 0; i < ActorCount; i++)
                {
                    finished[i] = actors[i].Ask<Finished>(Finish.Instance);
                }

                Task.WaitAll(finished);

                stopwatch.Stop();
                var elapsed = stopwatch.ElapsedMilliseconds;

                Console.WriteLine($"{ActorCount} actors stored {MessagesPerActor} events each in {elapsed / 1000.0} sec. Average: {ActorCount * MessagesPerActor * 1000.0 / elapsed} events/sec");

                //if (finished.Cast<Task<Finished>>().Any(task => !task.IsCompleted || task.Result.State != MessagesPerActor))
                //{
                //    throw new IllegalStateException("Actor's state was invalid");
                //}
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}