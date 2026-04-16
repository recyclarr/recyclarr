using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing;
using YamlDotNet.Core;

namespace Recyclarr.Core.Tests.Config.Parsing;

internal sealed class ConfigYamlExtensionsTest
{
    [Test]
    [TestCase("prefer_and_upgrade", PropersAndRepacksMode.PreferAndUpgrade)]
    [TestCase("do_not_upgrade", PropersAndRepacksMode.DoNotUpgrade)]
    [TestCase("do_not_prefer", PropersAndRepacksMode.DoNotPrefer)]
    public void Valid_propers_and_repacks_value_parses_correctly(
        string yamlValue,
        PropersAndRepacksMode expected
    )
    {
        var yaml = new MediaManagementConfigYaml { PropersAndRepacks = yamlValue };
        var serviceYaml = new RadarrConfigYaml { MediaManagement = yaml };

        var result = serviceYaml.ToRadarrConfiguration("test", null);

        result.MediaManagement.PropersAndRepacks.Should().Be(expected);
    }

    [Test]
    [TestCase("PREFER_AND_UPGRADE")]
    [TestCase("Prefer_And_Upgrade")]
    public void Propers_and_repacks_value_is_case_insensitive(string yamlValue)
    {
        var yaml = new MediaManagementConfigYaml { PropersAndRepacks = yamlValue };
        var serviceYaml = new RadarrConfigYaml { MediaManagement = yaml };

        var result = serviceYaml.ToRadarrConfiguration("test", null);

        result
            .MediaManagement.PropersAndRepacks.Should()
            .Be(PropersAndRepacksMode.PreferAndUpgrade);
    }

    [Test]
    public void Invalid_propers_and_repacks_value_throws_yaml_exception()
    {
        var yaml = new MediaManagementConfigYaml { PropersAndRepacks = "invalid_value" };
        var serviceYaml = new RadarrConfigYaml { MediaManagement = yaml };

        var act = () => serviceYaml.ToRadarrConfiguration("test", null);

        act.Should().Throw<YamlException>().WithMessage("*Invalid propers_and_repacks value*");
    }

    [Test]
    public void Null_propers_and_repacks_results_in_null()
    {
        var yaml = new MediaManagementConfigYaml { PropersAndRepacks = null };
        var serviceYaml = new RadarrConfigYaml { MediaManagement = yaml };

        var result = serviceYaml.ToRadarrConfiguration("test", null);

        result.MediaManagement.PropersAndRepacks.Should().BeNull();
    }

    [Test]
    public void Omitted_media_management_results_in_null_propers_and_repacks()
    {
        var serviceYaml = new RadarrConfigYaml { MediaManagement = null };

        var result = serviceYaml.ToRadarrConfiguration("test", null);

        result.MediaManagement.PropersAndRepacks.Should().BeNull();
    }

    [Test]
    public void Sonarr_parses_propers_and_repacks_correctly()
    {
        var yaml = new MediaManagementConfigYaml { PropersAndRepacks = "do_not_prefer" };
        var serviceYaml = new SonarrConfigYaml { MediaManagement = yaml };

        var result = serviceYaml.ToSonarrConfiguration("test", null);

        result.MediaManagement.PropersAndRepacks.Should().Be(PropersAndRepacksMode.DoNotPrefer);
    }

    [Test]
    public void Entry_level_score_applies_as_default_to_all_profiles()
    {
        var serviceYaml = new RadarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    Score = 42,
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "Profile A" },
                        new QualityScoreConfigYaml { Name = "Profile B" },
                    ],
                },
            ],
        };

        var result = serviceYaml.ToRadarrConfiguration("test", null);

        result
            .CustomFormats.Should()
            .ContainSingle()
            .Which.AssignScoresTo.Should()
            .BeEquivalentTo([
                new AssignScoresToConfig { Name = "Profile A", Score = 42 },
                new AssignScoresToConfig { Name = "Profile B", Score = 42 },
            ]);
    }

    [Test]
    public void Per_profile_score_overrides_entry_level_score()
    {
        var serviceYaml = new RadarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    Score = 42,
                    AssignScoresTo =
                    [
                        new QualityScoreConfigYaml { Name = "Profile A" },
                        new QualityScoreConfigYaml { Name = "Profile B", Score = 100 },
                    ],
                },
            ],
        };

        var result = serviceYaml.ToRadarrConfiguration("test", null);

        result
            .CustomFormats.Should()
            .ContainSingle()
            .Which.AssignScoresTo.Should()
            .BeEquivalentTo([
                new AssignScoresToConfig { Name = "Profile A", Score = 42 },
                new AssignScoresToConfig { Name = "Profile B", Score = 100 },
            ]);
    }

    [Test]
    public void Omitted_entry_level_score_leaves_per_profile_score_null()
    {
        var serviceYaml = new RadarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "Profile A" }],
                },
            ],
        };

        var result = serviceYaml.ToRadarrConfiguration("test", null);

        result
            .CustomFormats.Should()
            .ContainSingle()
            .Which.AssignScoresTo.Should()
            .ContainSingle()
            .Which.Score.Should()
            .BeNull();
    }

    [Test]
    public void Entry_level_score_works_for_sonarr()
    {
        var serviceYaml = new SonarrConfigYaml
        {
            CustomFormats =
            [
                new CustomFormatConfigYaml
                {
                    TrashIds = ["cf1"],
                    Score = -10,
                    AssignScoresTo = [new QualityScoreConfigYaml { Name = "Profile A" }],
                },
            ],
        };

        var result = serviceYaml.ToSonarrConfiguration("test", null);

        result
            .CustomFormats.Should()
            .ContainSingle()
            .Which.AssignScoresTo.Should()
            .ContainSingle()
            .Which.Score.Should()
            .Be(-10);
    }
}
