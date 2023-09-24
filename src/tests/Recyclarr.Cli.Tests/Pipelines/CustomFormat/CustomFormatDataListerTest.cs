using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Tests.TestLibrary;
using Recyclarr.TrashGuide.CustomFormat;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatDataListerTest
{
    [Test, AutoMockData]
    public void Custom_formats_appear_in_console_output(
        [Frozen(Matching.ImplementedInterfaces)] TestConsole console,
        [Frozen] ICustomFormatGuideService guide,
        IListCustomFormatSettings settings,
        CustomFormatDataLister sut)
    {
        var testData = new[]
        {
            NewCf.Data("First", "123"),
            NewCf.Data("Second", "456")
        };

        guide.GetCustomFormatData(default!).ReturnsForAnyArgs(testData);
        settings.ScoreSets.Returns(false);

        sut.List(settings);

        console.Output.Should().ContainAll(
            testData.SelectMany(x => new[] {x.Name, x.TrashId}));
    }
}
