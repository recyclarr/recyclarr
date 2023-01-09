using AutoFixture.NUnit3;
using FluentAssertions;
using NUnit.Framework;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Services.Common;
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
        GuideDataLister sut)
    {
        var testData = new[]
        {
            NewCf.Data("First", "123"),
            NewCf.Data("Second", "456")
        };

        sut.ListCustomFormats(testData);

        console.Output.Should().ContainAll(
            testData.SelectMany(x => new[] {x.Name, x.TrashId}));
    }
}
