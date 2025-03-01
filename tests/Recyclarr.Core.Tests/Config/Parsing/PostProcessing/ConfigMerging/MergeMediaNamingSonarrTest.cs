using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Core.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
public class MergeMediaNamingSonarrTest
{
    [Test]
    public void Empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            MediaNaming = new SonarrMediaNamingConfigYaml
            {
                Series = "series1",
                Season = "season1",
                Episodes = new SonarrEpisodeNamingConfigYaml
                {
                    Rename = false,
                    Standard = "standard1",
                    Daily = "daily1",
                    Anime = "anime1",
                },
            },
        };

        var rightConfig = new SonarrConfigYaml();

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Non_empty_right_to_empty_left()
    {
        var leftConfig = new SonarrConfigYaml();

        var rightConfig = new SonarrConfigYaml
        {
            MediaNaming = new SonarrMediaNamingConfigYaml
            {
                Series = "series1",
                Season = "season1",
                Episodes = new SonarrEpisodeNamingConfigYaml
                {
                    Rename = false,
                    Standard = "standard1",
                    Daily = "daily1",
                    Anime = "anime1",
                },
            },
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }

    [Test]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public void Non_empty_right_to_non_empty_left()
    {
        var leftConfig = new SonarrConfigYaml
        {
            MediaNaming = new SonarrMediaNamingConfigYaml
            {
                Series = "series1",
                Season = "season1",
                Episodes = new SonarrEpisodeNamingConfigYaml
                {
                    Rename = false,
                    Standard = "standard1",
                    Daily = "daily1",
                    Anime = "anime1",
                },
            },
        };

        var rightConfig = new SonarrConfigYaml
        {
            MediaNaming = new SonarrMediaNamingConfigYaml
            {
                Series = "series2",
                Season = "season2",
                Episodes = new SonarrEpisodeNamingConfigYaml
                {
                    Rename = false,
                    Standard = "standard2",
                    Daily = "daily2",
                    Anime = "anime2",
                },
            },
        };

        var sut = new SonarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }
}
