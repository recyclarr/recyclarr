using FluentAssertions;
using FluentAssertions.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.NSubstitute;
using TrashLib.Radarr.CustomFormat.Api;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Models.Cache;
using TrashLib.Radarr.CustomFormat.Processors.PersistenceSteps;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.Radarr.CustomFormat.Processors.PersistenceSteps;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityProfileApiPersistenceStepTest
{
    [Test]
    public async Task Do_not_invoke_api_if_no_scores_to_update()
    {
        const string radarrQualityProfileData = @"[{
  'name': 'profile1',
  'formatItems': [{
      'format': 1,
      'name': 'cf1',
      'score': 1
    },
    {
      'format': 2,
      'name': 'cf2',
      'score': 0
    },
    {
      'format': 3,
      'name': 'cf3',
      'score': 3
    }
  ],
  'id': 1
}]";

        var api = Substitute.For<IQualityProfileService>();
        api.GetQualityProfiles()!.Returns(JsonConvert.DeserializeObject<List<JObject>>(radarrQualityProfileData));

        var cfScores = new Dictionary<string, QualityProfileCustomFormatScoreMapping>
        {
            {
                "profile1", CfTestUtils.NewMapping(new FormatMappingEntry(
                    NewCf.Processed("", "", "", new TrashIdMapping("", "") {CustomFormatId = 4}), 100))
            }
        };

        var processor = new QualityProfileApiPersistenceStep();
        await processor.Process(api, cfScores);

        await api.DidNotReceive().UpdateQualityProfile(Arg.Any<JObject>(), Arg.Any<int>());
    }

    [Test]
    public async Task Invalid_quality_profile_names_are_reported()
    {
        const string radarrQualityProfileData = @"[{'name': 'profile1'}]";

        var api = Substitute.For<IQualityProfileService>();
        api.GetQualityProfiles()!.Returns(JsonConvert.DeserializeObject<List<JObject>>(radarrQualityProfileData));

        var cfScores = new Dictionary<string, QualityProfileCustomFormatScoreMapping>
        {
            {"wrong_profile_name", CfTestUtils.NewMapping()}
        };

        var processor = new QualityProfileApiPersistenceStep();
        await processor.Process(api, cfScores);

        processor.InvalidProfileNames.Should().Equal("wrong_profile_name");
        processor.UpdatedScores.Should().BeEmpty();
    }

    [Test]
    public async Task Reset_scores_for_unmatched_cfs_if_enabled()
    {
        const string radarrQualityProfileData = @"[{
  'name': 'profile1',
  'formatItems': [{
      'format': 1,
      'name': 'cf1',
      'score': 1
    },
    {
      'format': 2,
      'name': 'cf2',
      'score': 50
    },
    {
      'format': 3,
      'name': 'cf3',
      'score': 3
    }
  ],
  'id': 1
}]";

        var api = Substitute.For<IQualityProfileService>();
        api.GetQualityProfiles()!.Returns(JsonConvert.DeserializeObject<List<JObject>>(radarrQualityProfileData));

        var cfScores = new Dictionary<string, QualityProfileCustomFormatScoreMapping>
        {
            {
                "profile1", CfTestUtils.NewMappingWithReset(
                    new FormatMappingEntry(NewCf.Processed("", "", "", new TrashIdMapping("", "", 2)), 100))
            }
        };

        var processor = new QualityProfileApiPersistenceStep();
        await processor.Process(api, cfScores);

        processor.InvalidProfileNames.Should().BeEmpty();
        processor.UpdatedScores.Should()
            .ContainKey("profile1").WhoseValue.Should()
            .BeEquivalentTo(new List<UpdatedFormatScore>
            {
                new("cf1", 0, FormatScoreUpdateReason.Reset),
                new("cf2", 100, FormatScoreUpdateReason.Updated),
                new("cf3", 0, FormatScoreUpdateReason.Reset)
            });

        await api.Received().UpdateQualityProfile(
            Verify.That<JObject>(j => j["formatItems"]!.Children().Should().HaveCount(3)),
            Arg.Any<int>());
    }

    [Test]
    public async Task Scores_are_set_in_quality_profile()
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
        api.GetQualityProfiles()!.Returns(JsonConvert.DeserializeObject<List<JObject>>(radarrQualityProfileData));

        var cfScores = new Dictionary<string, QualityProfileCustomFormatScoreMapping>
        {
            {
                "profile1", CfTestUtils.NewMapping(
                    // First match by ID
                    new FormatMappingEntry(NewCf.Processed("", "", "", new TrashIdMapping("", "", 4)), 100),
                    // Should NOT match because we do not use names to assign scores
                    new FormatMappingEntry(NewCf.Processed("", "", "", new TrashIdMapping("", "BR-DISK")), 101),
                    // Second match by ID
                    new FormatMappingEntry(NewCf.Processed("", "", "", new TrashIdMapping("", "", 1)), 102))
            }
        };

        var processor = new QualityProfileApiPersistenceStep();
        await processor.Process(api, cfScores);

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

        await api.Received()
            .UpdateQualityProfile(Verify.That<JObject>(a => a.Should().BeEquivalentTo(expectedProfileJson)), 1);
        processor.InvalidProfileNames.Should().BeEmpty();
        processor.UpdatedScores.Should()
            .ContainKey("profile1").WhoseValue.Should()
            .BeEquivalentTo(new List<UpdatedFormatScore>
            {
                new("3D", 100, FormatScoreUpdateReason.Updated),
                new("asdf2", 102, FormatScoreUpdateReason.Updated)
            });
    }
}
