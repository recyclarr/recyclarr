using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Logging;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide.CustomFormat;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat;

internal sealed class CustomFormatDataListerTest
{
    [Test, AutoMockData]
    public void Custom_formats_appear_in_console_output(
        [Frozen(Matching.ImplementedInterfaces)] TestConsole console,
        [Frozen] ICustomFormatGuideService guide,
        IListCustomFormatSettings settings,
        IConsoleOutputSettings outputSettings,
        CustomFormatDataLister sut
    )
    {
        var testData = new[] { NewCf.Data("First", "123"), NewCf.Data("Second", "456") };

        guide.GetCustomFormatData(default!).ReturnsForAnyArgs(testData);
        settings.ScoreSets.Returns(false);

        sut.List(outputSettings, settings);

        console.Output.Should().ContainAll(testData.SelectMany(x => new[] { x.Name, x.TrashId }));
    }
}
