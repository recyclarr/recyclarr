using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Pipelines.CustomFormat.Guide;
using Recyclarr.TrashLib.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Pipelines.CustomFormat.PipelinePhases;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatConfigPhaseTest
{
    [Test, AutoMockData]
    public void Return_configs_that_exist_in_guide(
        [Frozen] ICustomFormatGuideService guide,
        CustomFormatConfigPhase sut)
    {
        guide.GetCustomFormatData(default!).ReturnsForAnyArgs(new[]
        {
            NewCf.Data("one", "cf1"),
            NewCf.Data("two", "cf2")
        });

        var config = new TestConfig
        {
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new List<string>
                    {
                        "cf1",
                        "cf2"
                    }
                }
            }
        };

        var result = sut.Execute(config);

        result.Should().BeEquivalentTo(new[]
        {
            NewCf.Data("one", "cf1"),
            NewCf.Data("two", "cf2")
        });
    }

    [Test, AutoMockData]
    public void Skip_configs_that_do_not_exist_in_guide(
        [Frozen] ICustomFormatGuideService guide,
        CustomFormatConfigPhase sut)
    {
        guide.GetCustomFormatData(default!).ReturnsForAnyArgs(new[]
        {
            NewCf.Data("", "cf4")
        });

        var config = new TestConfig
        {
            CustomFormats = new List<CustomFormatConfig>
            {
                new()
                {
                    TrashIds = new List<string>
                    {
                        "cf1",
                        "cf2",
                        "cf3"
                    }
                }
            }
        };

        var result = sut.Execute(config);

        result.Should().BeEmpty();
    }
}
