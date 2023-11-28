using System.Diagnostics.CodeAnalysis;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

namespace Recyclarr.Tests.Config.Parsing.PostProcessing.ConfigMerging;

[TestFixture]
public class MergeMediaNamingRadarrTest
{
    [Test]
    public void Empty_right_to_non_empty_left()
    {
        var leftConfig = new RadarrConfigYaml
        {
            MediaNaming = new RadarrMediaNamingConfigYaml
            {
                Folder = "folder1",
                Movie = new RadarrMovieNamingConfigYaml
                {
                    Rename = false,
                    Standard = "format1"
                }
            }
        };

        var rightConfig = new RadarrConfigYaml();

        var sut = new RadarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(leftConfig);
    }

    [Test]
    public void Non_empty_right_to_empty_left()
    {
        var leftConfig = new RadarrConfigYaml();

        var rightConfig = new RadarrConfigYaml
        {
            MediaNaming = new RadarrMediaNamingConfigYaml
            {
                Folder = "folder1",
                Movie = new RadarrMovieNamingConfigYaml
                {
                    Rename = false,
                    Standard = "format1"
                }
            }
        };

        var sut = new RadarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }

    [Test]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    public void Non_empty_right_to_non_empty_left()
    {
        var leftConfig = new RadarrConfigYaml
        {
            MediaNaming = new RadarrMediaNamingConfigYaml
            {
                Folder = "folder1",
                Movie = new RadarrMovieNamingConfigYaml
                {
                    Rename = false,
                    Standard = "format1"
                }
            }
        };

        var rightConfig = new RadarrConfigYaml
        {
            MediaNaming = new RadarrMediaNamingConfigYaml
            {
                Folder = "folder2",
                Movie = new RadarrMovieNamingConfigYaml
                {
                    Rename = false,
                    Standard = "format2"
                }
            }
        };

        var sut = new RadarrConfigMerger();

        var result = sut.Merge(leftConfig, rightConfig);

        result.Should().BeEquivalentTo(rightConfig);
    }
}
