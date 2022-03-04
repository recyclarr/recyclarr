using FluentAssertions;
using NUnit.Framework;
using TrashLib.Sonarr.ReleaseProfile;

namespace TrashLib.Tests.Sonarr.ReleaseProfile;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class UtilsTest
{
    [Test]
    public void Profile_with_only_ignored_should_not_be_filtered_out()
    {
        var profileData = new ProfileData {Ignored = new List<string> {"term"}};
        var data = new Dictionary<string, ProfileData> {{"actualData", profileData}};

        var filteredData = Utils.FilterProfiles(data);

        filteredData.Should().BeEquivalentTo(data);
    }

    [Test]
    public void Profile_with_only_required_should_not_be_filtered_out()
    {
        var profileData = new ProfileData {Required = new List<string> {"term"}};
        var data = new Dictionary<string, ProfileData> {{"actualData", profileData}};

        var filteredData = Utils.FilterProfiles(data);

        filteredData.Should().BeEquivalentTo(data);
    }

    [Test]
    public void Profile_with_only_preferred_should_not_be_filtered_out()
    {
        var profileData = new ProfileData
        {
            Preferred = new Dictionary<int, List<string>>
            {
                {100, new List<string> {"term"}}
            }
        };

        var data = new Dictionary<string, ProfileData> {{"actualData", profileData}};

        var filteredData = Utils.FilterProfiles(data);

        filteredData.Should().BeEquivalentTo(data);
    }

    [Test]
    public void Profile_with_only_optional_ignored_should_not_be_filtered_out()
    {
        var profileData = new ProfileData
        {
            Optional = new ProfileDataOptional
            {
                Ignored = new List<string> {"term"}
            }
        };

        var data = new Dictionary<string, ProfileData> {{"actualData", profileData}};

        var filteredData = Utils.FilterProfiles(data);

        filteredData.Should().BeEquivalentTo(data);
    }

    [Test]
    public void Profile_with_only_optional_required_should_not_be_filtered_out()
    {
        var profileData = new ProfileData
        {
            Optional = new ProfileDataOptional
            {
                Required = new List<string> {"required1"}
            }
        };

        var data = new Dictionary<string, ProfileData>
        {
            {"actualData", profileData}
        };

        var filteredData = Utils.FilterProfiles(data);

        filteredData.Should().BeEquivalentTo(data);
    }

    [Test]
    public void Profile_with_only_optional_preferred_should_not_be_filtered_out()
    {
        var profileData = new ProfileData
        {
            Optional = new ProfileDataOptional
            {
                Preferred = new Dictionary<int, List<string>>
                {
                    {100, new List<string> {"term"}}
                }
            }
        };

        var data = new Dictionary<string, ProfileData> {{"actualData", profileData}};

        var filteredData = Utils.FilterProfiles(data);

        filteredData.Should().BeEquivalentTo(data);
    }

    [Test]
    public void Empty_profiles_should_be_filtered_out()
    {
        var data = new Dictionary<string, ProfileData>
        {
            {"emptyData", new ProfileData()}
        };

        var filteredData = Utils.FilterProfiles(data);

        filteredData.Should().NotContainKey("emptyData");
    }
}
