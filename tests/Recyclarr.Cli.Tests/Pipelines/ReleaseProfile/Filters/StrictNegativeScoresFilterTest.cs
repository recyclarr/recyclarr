using Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.Cli.Tests.Pipelines.ReleaseProfile.Filters;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class StrictNegativeScoresFilterTest
{
    private static readonly ReleaseProfileData TestProfile = new()
    {
        Preferred = new[]
        {
            new PreferredTermData
            {
                Score = -1,
                Terms = new[]
                {
                    new TermData
                    {
                        TrashId = "abc",
                        Term = "a"
                    }
                }
            }
        }
    };

    [Test, AutoMockData]
    public void Preferred_with_negative_scores_is_treated_as_ignored_when_strict_negative_scores_enabled(
        StrictNegativeScoresFilter sut)
    {
        var config = new ReleaseProfileConfig
        {
            StrictNegativeScores = true
        };

        var result = sut.Transform(TestProfile, config);

        result.Preferred.Should().BeEmpty();
        result.Ignored.Should().BeEquivalentTo(TestProfile.Preferred.First().Terms);
    }

    [Test, AutoMockData]
    public void Preferred_and_ignored_untouched_when_strict_negative_scores_disabled(StrictNegativeScoresFilter sut)
    {
        var config = new ReleaseProfileConfig
        {
            StrictNegativeScores = false
        };

        var result = sut.Transform(TestProfile, config);
        result.Should().BeSameAs(TestProfile);
    }
}
