using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Core.Tests.Config.Parsing.PostProcessing.ConfigMerging;

internal sealed class MergeMediaManagementTest
{
    [Test]
    public void Empty_right_to_non_empty_left_preserves_left()
    {
        var leftConfig = new RadarrConfigYaml
        {
            MediaManagement = new MediaManagementConfigYaml
            {
                PropersAndRepacks = "prefer_and_upgrade",
            },
        };

        var rightConfig = new RadarrConfigYaml();

        var sut = new RadarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.MediaManagement.Should().BeEquivalentTo(leftConfig.MediaManagement);
    }

    [Test]
    public void Non_empty_right_to_empty_left_uses_right()
    {
        var leftConfig = new RadarrConfigYaml();

        var rightConfig = new RadarrConfigYaml
        {
            MediaManagement = new MediaManagementConfigYaml
            {
                PropersAndRepacks = "do_not_upgrade",
            },
        };

        var sut = new RadarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.MediaManagement.Should().BeEquivalentTo(rightConfig.MediaManagement);
    }

    [Test]
    public void Non_empty_right_overrides_non_empty_left()
    {
        var leftConfig = new RadarrConfigYaml
        {
            MediaManagement = new MediaManagementConfigYaml
            {
                PropersAndRepacks = "prefer_and_upgrade",
            },
        };

        var rightConfig = new RadarrConfigYaml
        {
            MediaManagement = new MediaManagementConfigYaml { PropersAndRepacks = "do_not_prefer" },
        };

        var sut = new RadarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.MediaManagement.Should().BeEquivalentTo(rightConfig.MediaManagement);
    }

    [Test]
    public void Null_right_propers_and_repacks_preserves_left_value()
    {
        var leftConfig = new RadarrConfigYaml
        {
            MediaManagement = new MediaManagementConfigYaml
            {
                PropersAndRepacks = "prefer_and_upgrade",
            },
        };

        var rightConfig = new RadarrConfigYaml
        {
            MediaManagement = new MediaManagementConfigYaml { PropersAndRepacks = null },
        };

        var sut = new RadarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.MediaManagement?.PropersAndRepacks.Should().Be("prefer_and_upgrade");
    }

    [Test]
    public void Sonarr_merging_follows_same_override_pattern()
    {
        var leftConfig = new SonarrConfigYaml
        {
            MediaManagement = new MediaManagementConfigYaml
            {
                PropersAndRepacks = "prefer_and_upgrade",
            },
        };

        var rightConfig = new SonarrConfigYaml
        {
            MediaManagement = new MediaManagementConfigYaml
            {
                PropersAndRepacks = "do_not_upgrade",
            },
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.MediaManagement.Should().BeEquivalentTo(rightConfig.MediaManagement);
    }
}
