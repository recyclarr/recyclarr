using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using Trash.Config;
using Trash.Radarr;
using Trash.Radarr.CustomFormat.Api;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;
using Trash.Radarr.CustomFormat.Processors;

namespace Trash.Tests.Radarr.CustomFormat.Processors
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class PersistenceProcessorTest
    {
        [Test]
        public void Custom_formats_are_deleted_if_deletion_option_is_enabled_in_config()
        {
            var steps = Substitute.For<IPersistenceProcessorSteps>();
            var cfApi = Substitute.For<ICustomFormatService>();
            var qpApi = Substitute.For<IQualityProfileService>();

            var configProvider = Substitute.For<IConfigurationProvider>();
            configProvider.ActiveConfiguration = new RadarrConfiguration {DeleteOldCustomFormats = true};

            var guideCfs = Array.Empty<ProcessedCustomFormatData>();
            var deletedCfsInCache = new Collection<TrashIdMapping>();
            var profileScores = new Dictionary<string, List<QualityProfileCustomFormatScoreEntry>>();

            var processor = new PersistenceProcessor(cfApi, qpApi, configProvider, () => steps);
            processor.PersistCustomFormats(guideCfs, deletedCfsInCache, profileScores);

            steps.JsonTransactionStep.Received().RecordDeletions(Arg.Is(deletedCfsInCache), Arg.Any<List<JObject>>());
        }

        [Test]
        public void Custom_formats_are_not_deleted_if_deletion_option_is_disabled_in_config()
        {
            var steps = Substitute.For<IPersistenceProcessorSteps>();
            var cfApi = Substitute.For<ICustomFormatService>();
            var qpApi = Substitute.For<IQualityProfileService>();

            var configProvider = Substitute.For<IConfigurationProvider>();
            configProvider.ActiveConfiguration = new RadarrConfiguration {DeleteOldCustomFormats = false};

            var guideCfs = Array.Empty<ProcessedCustomFormatData>();
            var deletedCfsInCache = Array.Empty<TrashIdMapping>();
            var profileScores = new Dictionary<string, List<QualityProfileCustomFormatScoreEntry>>();

            var processor = new PersistenceProcessor(cfApi, qpApi, configProvider, () => steps);
            processor.PersistCustomFormats(guideCfs, deletedCfsInCache, profileScores);

            steps.JsonTransactionStep.DidNotReceive()
                .RecordDeletions(Arg.Any<IEnumerable<TrashIdMapping>>(), Arg.Any<List<JObject>>());
        }

        [Test]
        public void Different_active_configuration_is_properly_used()
        {
            var steps = Substitute.For<IPersistenceProcessorSteps>();
            var cfApi = Substitute.For<ICustomFormatService>();
            var qpApi = Substitute.For<IQualityProfileService>();
            var configProvider = Substitute.For<IConfigurationProvider>();

            var guideCfs = Array.Empty<ProcessedCustomFormatData>();
            var deletedCfsInCache = Array.Empty<TrashIdMapping>();
            var profileScores = new Dictionary<string, List<QualityProfileCustomFormatScoreEntry>>();

            var processor = new PersistenceProcessor(cfApi, qpApi, configProvider, () => steps);

            configProvider.ActiveConfiguration = new RadarrConfiguration {DeleteOldCustomFormats = false};
            processor.PersistCustomFormats(guideCfs, deletedCfsInCache, profileScores);

            configProvider.ActiveConfiguration = new RadarrConfiguration {DeleteOldCustomFormats = true};
            processor.PersistCustomFormats(guideCfs, deletedCfsInCache, profileScores);

            steps.JsonTransactionStep.Received(1)
                .RecordDeletions(Arg.Any<IEnumerable<TrashIdMapping>>(), Arg.Any<List<JObject>>());
        }
    }
}
