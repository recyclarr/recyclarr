using AutoFixture.NUnit3;
using CliFx.Infrastructure;
using FluentAssertions;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Services.Common;
using TrashLib.TestLibrary;

namespace TrashLib.Tests.CustomFormat;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class GuideDataListerTest
{
    [Test, AutoMockData]
    public void Custom_formats_appear_in_console_output(
        [Frozen(Matching.ImplementedInterfaces)] FakeInMemoryConsole console,
        GuideDataLister sut)
    {
        var testData = new[]
        {
            NewCf.Data("First", "123"),
            NewCf.Data("Second", "456")
        };

        sut.ListCustomFormats(testData);

        console.ReadOutputString().Should().ContainAll(
            testData.SelectMany(x => new[] {x.Name, x.TrashId}));
    }
}
