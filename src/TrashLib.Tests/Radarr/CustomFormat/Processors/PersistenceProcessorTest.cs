using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;
using TrashLib.Radarr.CustomFormat.Processors;

namespace TrashLib.Tests.Radarr.CustomFormat.Processors
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class PersistenceProcessorTest
    {
        private class Context
        {
            public Context()
            {
                Steps = Substitute.For<IPersistenceProcessorSteps>();
                GuideCfs = Array.Empty<ProcessedCustomFormatData>();
                DeletedCfsInCache = new Collection<TrashIdMapping>();
                ProfileScores = new Dictionary<string, QualityProfileCustomFormatScoreMapping>();

                Processor = new PersistenceProcessor(
                    _ => Substitute.For<ICustomFormatService>(),
                    _ => Substitute.For<IQualityProfileService>(),
                    () => Steps);
            }

            public PersistenceProcessor Processor { get; }
            public Dictionary<string, QualityProfileCustomFormatScoreMapping> ProfileScores { get; }
            public Collection<TrashIdMapping> DeletedCfsInCache { get; }
            public ProcessedCustomFormatData[] GuideCfs { get; }
            public IPersistenceProcessorSteps Steps { get; }
        }

        [Test]
        public void Custom_formats_are_deleted_if_deletion_option_is_enabled_in_config()
        {
            var config = new RadarrConfiguration {DeleteOldCustomFormats = true};
            var ctx = new Context();

            ctx.Processor.PersistCustomFormats(config, ctx.GuideCfs, ctx.DeletedCfsInCache, ctx.ProfileScores);
            ctx.Steps.JsonTransactionStep.Received()
                .RecordDeletions(Arg.Is(ctx.DeletedCfsInCache), Arg.Any<List<JObject>>());
        }

        [Test]
        public void Custom_formats_are_not_deleted_if_deletion_option_is_disabled_in_config()
        {
            var config = new RadarrConfiguration {DeleteOldCustomFormats = false};
            var ctx = new Context();

            ctx.Processor.PersistCustomFormats(config, ctx.GuideCfs, ctx.DeletedCfsInCache, ctx.ProfileScores);
            ctx.Steps.JsonTransactionStep.DidNotReceive()
                .RecordDeletions(Arg.Any<IEnumerable<TrashIdMapping>>(), Arg.Any<List<JObject>>());
        }

        [Test]
        public void Different_active_configuration_is_properly_used()
        {
            var ctx = new Context();

            var config = new RadarrConfiguration {DeleteOldCustomFormats = false};
            ctx.Processor.PersistCustomFormats(config, ctx.GuideCfs, ctx.DeletedCfsInCache, ctx.ProfileScores);

            config = new RadarrConfiguration {DeleteOldCustomFormats = true};
            ctx.Processor.PersistCustomFormats(config, ctx.GuideCfs, ctx.DeletedCfsInCache, ctx.ProfileScores);

            ctx.Steps.JsonTransactionStep.Received(1)
                .RecordDeletions(Arg.Any<IEnumerable<TrashIdMapping>>(), Arg.Any<List<JObject>>());
        }
    }
}
