using FluentAssertions;
using NUnit.Framework;
using TestLibrary.AutoFixture;
using TrashLib.Services.Sonarr.ReleaseProfile;
using TrashLib.Services.Sonarr.ReleaseProfile.Filters;

namespace TrashLib.Tests.Sonarr.ReleaseProfile.Filters;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ReleaseProfileDataValidationFiltererTest
{
    [Test, AutoMockData]
    public void Valid_data_is_not_filtered_out(ReleaseProfileDataValidationFilterer sut)
    {
        var data = new[]
        {
            new ReleaseProfileData
            {
                TrashId = "trash_id",
                Name = "name",
                Required = new[] {new TermData {Term = "term1"}},
                Ignored = new[] {new TermData {Term = "term2"}},
                Preferred = new[] {new PreferredTermData {Terms = new[] {new TermData {Term = "term3"}}}}
            }
        };

        var result = sut.FilterProfiles(data);

        result.Should().BeEquivalentTo(data);
    }

    [Test, AutoMockData]
    public void Invalid_terms_are_filtered_out(ReleaseProfileDataValidationFilterer sut)
    {
        var data = new[]
        {
            new ReleaseProfileData
            {
                TrashId = "trash_id",
                Name = "name",
                Required = new[] {new TermData {Term = ""}},
                Ignored = new[] {new TermData {Term = "term2"}},
                Preferred = new[] {new PreferredTermData {Terms = new[] {new TermData {Term = "term3"}}}}
            }
        };

        var result = sut.FilterProfiles(data);

        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new ReleaseProfileData
        {
            TrashId = "trash_id",
            Name = "name",
            Required = Array.Empty<TermData>(),
            Ignored = new[] {new TermData {Term = "term2"}},
            Preferred = new[] {new PreferredTermData {Terms = new[] {new TermData {Term = "term3"}}}}
        });
    }

    [Test, AutoMockData]
    public void Whole_release_profile_filtered_out_if_all_terms_invalid(ReleaseProfileDataValidationFilterer sut)
    {
        var data = new[]
        {
            new ReleaseProfileData
            {
                TrashId = "trash_id",
                Name = "name",
                Required = new[] {new TermData {Term = ""}},
                Ignored = new[] {new TermData {Term = ""}},
                Preferred = new[] {new PreferredTermData {Terms = new[] {new TermData {Term = ""}}}}
            }
        };

        var result = sut.FilterProfiles(data);

        result.Should().BeEmpty();
    }
}
