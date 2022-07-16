using AutoFixture.NUnit3;
using CliFx.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Radarr.CustomFormat;
using TrashLib.Radarr.CustomFormat.Guide;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.Radarr.CustomFormat;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class CustomFormatListerTest
{
    [Test, AutoMockData]
    public void Custom_formats_appear_in_console_output(
        [Frozen] IRadarrGuideService guide,
        [Frozen(Matching.ImplementedInterfaces)] FakeInMemoryConsole console,
        CustomFormatLister sut)
    {
        var testData = new[]
        {
            NewCf.Data("First", "file1", "123"),
            NewCf.Data("Second", "file2", "456")
        };

        guide.GetCustomFormatData().Returns(testData);

        sut.ListCustomFormats();

        console.ReadOutputString().Should()
            .ContainAll(testData.SelectMany(x => new[] {x.Name, x.TrashId}));
    }
}
