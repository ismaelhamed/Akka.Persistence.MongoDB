//-----------------------------------------------------------------------
// <copyright file="MongoDbJournalSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Configuration;
using Akka.Persistence.TestKit.Performance;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.MongoDb.Tests
{
    [Collection("MongoDbSpec")]
    public class MongoDbJournalPerfSpec : JournalPerfSpec, IClassFixture<DatabaseFixture>
    {
        private static Config CreateSpecConfig(DatabaseFixture databaseFixture)
        {
            var specString = @"
                akka.test.single-expect-default = 3s
                akka.persistence {
                    publish-plugin-commands = on
                    journal {
                        plugin = ""akka.persistence.journal.mongodb""
                        mongodb {
                            class = ""Akka.Persistence.MongoDb.Journal.MongoDbJournal, Akka.Persistence.MongoDb""
                            connection-string = """ + databaseFixture.ConnectionString + @"""
                            auto-initialize = on
                            collection = ""EventJournal""
                        }
                    }
                }";

            return ConfigurationFactory.ParseString(specString);
        }

        public MongoDbJournalPerfSpec(ITestOutputHelper output, DatabaseFixture databaseFixture)
            : base(CreateSpecConfig(databaseFixture), "MongoDbJournalPerfSpec", output)
        {
            EventsCount = 1000;
            ExpectDuration = TimeSpan.FromMinutes(10);
            MeasurementIterations = 10;
        }
    }
}
