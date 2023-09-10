using Recyclarr.Cli.Pipelines.ReleaseProfile;
using Recyclarr.TrashLib.Guide.ReleaseProfile;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.Tests.Pipelines.ReleaseProfile;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ReleaseProfileDataListerTest
{
    [Test, AutoMockData]
    public void Release_profiles_appear_in_console_output(
        [Frozen(Matching.ImplementedInterfaces)] TestConsole console,
        [Frozen] IReleaseProfileGuideService guide,
        ReleaseProfileDataLister sut)
    {
        var testData = new[]
        {
            new ReleaseProfileData {Name = "First", TrashId = "123"},
            new ReleaseProfileData {Name = "Second", TrashId = "456"}
        };

        guide.GetReleaseProfileData().Returns(testData);

        sut.ListReleaseProfiles();

        console.Output.Should().ContainAll(
            testData.SelectMany(x => new[] {x.Name, x.TrashId}));
    }

    [Test, AutoMockData]
    public void Terms_appear_in_console_output(
        [Frozen] IReleaseProfileGuideService guide,
        [Frozen(Matching.ImplementedInterfaces)] TestConsole console,
        ReleaseProfileDataLister sut)
    {
        var requiredData = new[]
        {
            new TermData {Name = "First", TrashId = "111", Term = "term1"},
            new TermData {Name = "Second", TrashId = "222", Term = "term2"}
        };

        var ignoredData = new[]
        {
            new TermData {Name = "Third", TrashId = "333", Term = "term3"},
            new TermData {Name = "Fourth", TrashId = "444", Term = "term4"}
        };

        var preferredData = new[]
        {
            new TermData {Name = "Fifth", TrashId = "555", Term = "term5"},
            new TermData {Name = "Sixth", TrashId = "666", Term = "term6"}
        };

        guide.GetReleaseProfileData().Returns(new[]
        {
            new ReleaseProfileData
            {
                Name = "Release Profile",
                TrashId = "098",
                Required = requiredData,
                Ignored = ignoredData,
                Preferred = new PreferredTermData[]
                {
                    new() {Score = 100, Terms = preferredData}
                }
            }
        });

        sut.ListTerms("098");

        var expectedOutput = new[]
        {
            requiredData.SelectMany(x => new[] {x.Name, x.TrashId}),
            ignoredData.SelectMany(x => new[] {x.Name, x.TrashId}),
            preferredData.SelectMany(x => new[] {x.Name, x.TrashId})
        };

        console.Output.Should().ContainAll(expectedOutput.SelectMany(x => x));
    }
}
