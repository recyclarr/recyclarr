using System.IO.Abstractions;
using Recyclarr.Core.TestLibrary;
using Recyclarr.TestLibrary;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Core.Tests.IntegrationTests;

[TestFixture]
public class QualitySizeGuideParserTest : IntegrationTestFixture
{
    [Test]
    public void Get_valid_data()
    {
        var qualityDir = Fs.CurrentDirectory().SubDirectory("json");
        Fs.AddSameFileFromEmbeddedResource(qualityDir.File("quality_size.json"), GetType());
        qualityDir.Refresh();

        var sut = Resolve<QualitySizeGuideParser>();

        var result = sut.GetQualities([qualityDir]);

        result
            .Should()
            .BeEquivalentTo(
                [
                    new QualitySizeData
                    {
                        Type = "series",
                        Qualities =
                        [
                            new QualityItem("quality1", 1, 2, 3),
                            new QualityItem("quality2", 4.1m, 5.1m, 6.1m),
                        ],
                    },
                ]
            );
    }

    [Test]
    public void Invalid_data_gets_skipped()
    {
        var qualityDir = Fs.CurrentDirectory().SubDirectory("json");
        Fs.AddSameFileFromEmbeddedResource(qualityDir.File("invalid_quality_size.json"), GetType());
        qualityDir.Refresh();

        var sut = Resolve<QualitySizeGuideParser>();

        var result = sut.GetQualities([qualityDir]);

        result.Should().BeEmpty();
    }
}
