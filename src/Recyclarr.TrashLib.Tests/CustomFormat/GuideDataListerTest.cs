using AutoFixture.NUnit3;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Services.CustomFormat.Guide;
using Recyclarr.TrashLib.TestLibrary;
using Spectre.Console.Testing;

namespace Recyclarr.TrashLib.Tests.CustomFormat;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class GuideDataListerTest
{
    [Test, AutoMockData]
    public void Custom_formats_appear_in_console_output(
        [Frozen(Matching.ImplementedInterfaces)] TestConsole console,
        [Frozen] ICustomFormatGuideService guide,
        CustomFormatDataLister sut)
    {
        var testData = new[]
        {
            NewCf.Data("First", "123"),
            NewCf.Data("Second", "456")
        };

        guide.GetCustomFormatData(default!).ReturnsForAnyArgs(testData);

        sut.ListCustomFormats(default!);

        console.Output.Should().ContainAll(
            testData.SelectMany(x => new[] {x.Name, x.TrashId}));
    }
}
