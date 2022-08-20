using FluentAssertions;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Sonarr.Config;
using TrashLib.Sonarr.ReleaseProfile;
using TrashLib.Sonarr.ReleaseProfile.Filters;

namespace TrashLib.Tests.Sonarr.ReleaseProfile.Filters;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ReleaseProfileDataFiltererTest
{
    [Test, AutoMockData]
    public void Include_terms_filter_works(ReleaseProfileDataFilterer sut)
    {
        var filter = new[] {"1", "2"};
        var terms = new TermData[]
        {
            new() {TrashId = "1", Term = "term1"},
            new() {TrashId = "2", Term = "term2"},
            new() {TrashId = "3", Term = "term3"},
            new() {Term = "term4"},
            new() {Term = "term5"}
        };

        var result = sut.IncludeTerms(terms, filter);

        result.Should().BeEquivalentTo(new TermData[]
        {
            new() {TrashId = "1", Term = "term1"},
            new() {TrashId = "2", Term = "term2"}
        });
    }

    [Test, AutoMockData]
    public void Include_preferred_terms_filter_works(ReleaseProfileDataFilterer sut)
    {
        var filter = new[] {"1", "2"};
        var terms = new PreferredTermData[]
        {
            new()
            {
                Score = 10, Terms = new TermData[]
                {
                    new() {TrashId = "1", Term = "term1"},
                    new() {TrashId = "2", Term = "term2"},
                    new() {TrashId = "3", Term = "term3"},
                    new() {Term = "term4"},
                    new() {Term = "term5"}
                }
            },
            new()
            {
                Score = 20, Terms = new TermData[]
                {
                    new() {TrashId = "4", Term = "term4"}
                }
            }
        };

        var result = sut.IncludeTerms(terms, filter);

        result.Should().BeEquivalentTo(new PreferredTermData[]
        {
            new()
            {
                Score = 10, Terms = new TermData[]
                {
                    new() {TrashId = "1", Term = "term1"},
                    new() {TrashId = "2", Term = "term2"}
                }
            }
        });
    }

    [Test, AutoMockData]
    public void Exclude_terms_filter_works(ReleaseProfileDataFilterer sut)
    {
        var filter = new[] {"1", "2"};
        var terms = new TermData[]
        {
            new() {TrashId = "1", Term = "term1"},
            new() {TrashId = "2", Term = "term2"},
            new() {TrashId = "3", Term = "term3"},
            new() {Term = "term4"},
            new() {Term = "term5"}
        };

        var result = sut.ExcludeTerms(terms, filter);

        result.Should().BeEquivalentTo(new TermData[]
        {
            new() {TrashId = "3", Term = "term3"},
            new() {Term = "term4"},
            new() {Term = "term5"}
        });
    }

    [Test, AutoMockData]
    public void Exclude_preferred_terms_filter_works(ReleaseProfileDataFilterer sut)
    {
        var filter = new[] {"1", "2"};
        var terms = new PreferredTermData[]
        {
            new()
            {
                Score = 10,
                Terms = new TermData[]
                {
                    new() {TrashId = "1", Term = "term1"},
                    new() {TrashId = "2", Term = "term2"},
                    new() {TrashId = "3", Term = "term3"},
                    new() {Term = "term4"},
                    new() {Term = "term5"}
                }
            },
            new()
            {
                Score = 20,
                Terms = new TermData[]
                {
                    new() {TrashId = "4", Term = "term4"},
                    new() {Term = "term6"},
                    new() {Term = "term7"}
                }
            }
        };

        var result = sut.ExcludeTerms(terms, filter);

        result.Should().BeEquivalentTo(new PreferredTermData[]
        {
            new()
            {
                Score = 10,
                Terms = new TermData[]
                {
                    new() {TrashId = "3", Term = "term3"},
                    new() {Term = "term4"},
                    new() {Term = "term5"}
                }
            },
            new()
            {
                Score = 20,
                Terms = new TermData[]
                {
                    new() {TrashId = "4", Term = "term4"},
                    new() {Term = "term6"},
                    new() {Term = "term7"}
                }
            }
        });
    }

    [Test, AutoMockData]
    public void Filter_profile_data_with_invalid_terms(ReleaseProfileDataFilterer sut)
    {
        var profileData = new ReleaseProfileData
        {
            Preferred = new PreferredTermData[]
            {
                new()
                {
                    Score = 10, Terms = new TermData[]
                    {
                        new() {TrashId = "1", Term = "term1"}, // excluded by filter
                        new() {TrashId = "2", Term = ""}, // excluded because it's invalid
                        new() {TrashId = "3", Term = "term3"}
                    }
                },
                new()
                {
                    Score = 20, Terms = new TermData[]
                    {
                        new() {TrashId = "4", Term = "term4"}
                    }
                }
            }
        };

        var filter = new SonarrProfileFilterConfig
        {
            Exclude = new[] {"1"}
        };

        var result = sut.FilterProfile(profileData, filter);

        result.Should().BeEquivalentTo(new ReleaseProfileData
        {
            Preferred = new PreferredTermData[]
            {
                new()
                {
                    Score = 10, Terms = new TermData[]
                    {
                        new() {TrashId = "3", Term = "term3"}
                    }
                },
                new()
                {
                    Score = 20, Terms = new TermData[]
                    {
                        new() {TrashId = "4", Term = "term4"}
                    }
                }
            }
        });
    }
}
