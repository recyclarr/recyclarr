using System.IO.Abstractions;
using Recyclarr.Cli.Pipelines.QualitySize;
using Recyclarr.Cli.Pipelines.QualitySize.Guide;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.Common.Extensions;
using Recyclarr.Common.TestLibrary;

namespace Recyclarr.Cli.Tests.Pipelines.QualitySize.Guide;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualitySizeGuideParserTest : CliIntegrationFixture
{
    [Test]
    public void Get_valid_data()
    {
        var qualityDir = Fs.CurrentDirectory().SubDir("json");
        Fs.AddSameFileFromEmbeddedResource(qualityDir.File("quality_size.json"), GetType());
        qualityDir.Refresh();

        var sut = Resolve<QualitySizeGuideParser>();

        var result = sut.GetQualities(new[] {qualityDir});

        result.Should().BeEquivalentTo(new[]
        {
            new QualitySizeData
            {
                Type = "series",
                Qualities = new[]
                {
                    new QualitySizeItem("quality1", 1, 2, 3),
                    new QualitySizeItem("quality2", 4.1m, 5.1m, 6.1m)
                }
            }
        });
    }

    [Test]
    public void Invalid_data_gets_skipped()
    {
        var qualityDir = Fs.CurrentDirectory().SubDir("json");
        Fs.AddSameFileFromEmbeddedResource(qualityDir.File("invalid_quality_size.json"), GetType());
        qualityDir.Refresh();

        var sut = Resolve<QualitySizeGuideParser>();

        var result = sut.GetQualities(new[] {qualityDir});

        result.Should().BeEmpty();
    }
}
