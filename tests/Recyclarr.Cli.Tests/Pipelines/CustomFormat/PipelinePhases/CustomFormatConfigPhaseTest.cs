using AutoFixture;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Config.Models;
using Recyclarr.Tests.TestLibrary;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.PipelinePhases;

[TestFixture]
public class CustomFormatConfigPhaseTest
{
    [Test]
    public void Return_configs_that_exist_in_guide()
    {
        var fixture = NSubstituteFixture.Create();

        var guide = fixture.Freeze<ICustomFormatGuideService>();
        guide.GetCustomFormatData(default!).ReturnsForAnyArgs([
            NewCf.Data("one", "cf1"),
            NewCf.Data("two", "cf2")
        ]);

        fixture.Inject<IServiceConfiguration>(NewConfig.Radarr() with
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
        });

        var context = new CustomFormatPipelineContext();
        var sut = fixture.Create<CustomFormatConfigPhase>();
        sut.Execute(context);

        context.ConfigOutput.Should().BeEquivalentTo([
            NewCf.Data("one", "cf1"),
            NewCf.Data("two", "cf2")
        ]);
    }

    [Test]
    public void Skip_configs_that_do_not_exist_in_guide()
    {
        var fixture = NSubstituteFixture.Create();

        var guide = fixture.Freeze<ICustomFormatGuideService>();
        guide.GetCustomFormatData(default!).ReturnsForAnyArgs([
            NewCf.Data("", "cf4")
        ]);

        fixture.Inject<IServiceConfiguration>(NewConfig.Radarr() with
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
        });

        var context = new CustomFormatPipelineContext();
        var sut = fixture.Create<CustomFormatConfigPhase>();
        sut.Execute(context);

        context.ConfigOutput.Should().BeEmpty();
    }
}
