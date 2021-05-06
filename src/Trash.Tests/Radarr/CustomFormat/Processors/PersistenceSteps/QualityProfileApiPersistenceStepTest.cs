using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.NSubstitute;
using Trash.Radarr.CustomFormat.Api;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;
using Trash.Radarr.CustomFormat.Processors.PersistenceSteps;

namespace Trash.Tests.Radarr.CustomFormat.Processors.PersistenceSteps
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class QualityProfileApiPersistenceStepTest
    {
        [Test]
        public void Invalid_quality_profile_names_are_reported()
        {
            const string radarrQualityProfileData = @"[{'name': 'profile1'}]";

            var api = Substitute.For<IQualityProfileService>();
            api.GetQualityProfiles().Returns(JsonConvert.DeserializeObject<List<JObject>>(radarrQualityProfileData));

            var cfScores = new Dictionary<string, List<QualityProfileCustomFormatScoreEntry>>
            {
                {"wrong_profile_name", new List<QualityProfileCustomFormatScoreEntry>()}
            };

            var processor = new QualityProfileApiPersistenceStep();
            processor.Process(api, cfScores);

            api.DidNotReceive().UpdateQualityProfile(Arg.Any<JObject>(), Arg.Any<int>());
            processor.InvalidProfileNames.Should().BeEquivalentTo("wrong_profile_name");
            processor.UpdatedScores.Should().BeEmpty();
        }

        [Test]
        public void Scores_are_set_in_quality_profile()
        {
            const string radarrQualityProfileData = @"[{
  'name': 'profile1',
  'upgradeAllowed': false,
  'cutoff': 20,
  'items': [{
      'quality': {
        'id': 10,
        'name': 'Raw-HD',
        'source': 'tv',
        'resolution': 1080,
        'modifier': 'rawhd'
      },
      'items': [],
      'allowed': false
    }
  ],
  'minFormatScore': 0,
  'cutoffFormatScore': 0,
  'formatItems': [{
      'format': 4,
      'name': '3D',
      'score': 0
    },
    {
      'format': 3,
      'name': 'BR-DISK',
      'score': 0
    },
    {
      'format': 1,
      'name': 'asdf2',
      'score': 0
    }
  ],
  'language': {
    'id': 1,
    'name': 'English'
  },
  'id': 1
}]";

            var api = Substitute.For<IQualityProfileService>();
            api.GetQualityProfiles().Returns(JsonConvert.DeserializeObject<List<JObject>>(radarrQualityProfileData));

            var cfScores = new Dictionary<string, List<QualityProfileCustomFormatScoreEntry>>
            {
                {
                    "profile1", new List<QualityProfileCustomFormatScoreEntry>
                    {
                        new(new ProcessedCustomFormatData("", "", new JObject())
                        {
                            // First match by ID
                            CacheEntry = new TrashIdMapping("", "") {CustomFormatId = 4}
                        }, 100),
                        new(new ProcessedCustomFormatData("", "", new JObject())
                        {
                            // Should NOT match because we do not use names to assign scores
                            CacheEntry = new TrashIdMapping("", "BR-DISK")
                        }, 101),
                        new(new ProcessedCustomFormatData("", "", new JObject())
                        {
                            // Second match by ID
                            CacheEntry = new TrashIdMapping("", "") {CustomFormatId = 1}
                        }, 102)
                    }
                }
            };

            var processor = new QualityProfileApiPersistenceStep();
            processor.Process(api, cfScores);

            var expectedProfileJson = JObject.Parse(@"{
  'name': 'profile1',
  'upgradeAllowed': false,
  'cutoff': 20,
  'items': [{
      'quality': {
        'id': 10,
        'name': 'Raw-HD',
        'source': 'tv',
        'resolution': 1080,
        'modifier': 'rawhd'
      },
      'items': [],
      'allowed': false
    }
  ],
  'minFormatScore': 0,
  'cutoffFormatScore': 0,
  'formatItems': [{
      'format': 4,
      'name': '3D',
      'score': 100
    },
    {
      'format': 3,
      'name': 'BR-DISK',
      'score': 0
    },
    {
      'format': 1,
      'name': 'asdf2',
      'score': 102
    }
  ],
  'language': {
    'id': 1,
    'name': 'English'
  },
  'id': 1
}");

            api.Received()
                .UpdateQualityProfile(Verify.That<JObject>(a => a.Should().BeEquivalentTo(expectedProfileJson)), 1);
            processor.InvalidProfileNames.Should().BeEmpty();
            processor.UpdatedScores.Should().ContainKey("profile1").WhichValue.Should().BeEquivalentTo(
                cfScores.Values.First()[0],
                cfScores.Values.First()[2]);
        }
    }
}
