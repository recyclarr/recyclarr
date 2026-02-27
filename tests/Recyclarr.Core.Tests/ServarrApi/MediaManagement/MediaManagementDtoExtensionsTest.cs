using Recyclarr.Config.Models;
using Recyclarr.Servarr.MediaManagement;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class MediaManagementDataDifferencesTest
{
    [Test]
    public void Returns_diff_when_value_changes()
    {
        var original = new MediaManagementData
        {
            PropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
        };
        var updated = new MediaManagementData
        {
            PropersAndRepacks = PropersAndRepacksMode.DoNotUpgrade,
        };

        var differences = original.GetDifferences(updated);

        differences.Should().HaveCount(1);
        differences.Should().Contain(d => d.Contains("DownloadPropersAndRepacks"));
        differences.Should().Contain(d => d.Contains("PreferAndUpgrade"));
        differences.Should().Contain(d => d.Contains("DoNotUpgrade"));
    }

    [Test]
    public void Returns_empty_when_values_match()
    {
        var original = new MediaManagementData
        {
            PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
        };
        var updated = new MediaManagementData
        {
            PropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
        };

        var differences = original.GetDifferences(updated);

        differences.Should().BeEmpty();
    }

    [Test]
    public void Returns_diff_when_old_is_null_and_new_is_set()
    {
        var original = new MediaManagementData { PropersAndRepacks = null };
        var updated = new MediaManagementData
        {
            PropersAndRepacks = PropersAndRepacksMode.DoNotUpgrade,
        };

        var differences = original.GetDifferences(updated);

        differences.Should().HaveCount(1);
    }

    [Test]
    public void Returns_empty_when_both_null()
    {
        var original = new MediaManagementData { PropersAndRepacks = null };
        var updated = new MediaManagementData { PropersAndRepacks = null };

        var differences = original.GetDifferences(updated);

        differences.Should().BeEmpty();
    }
}
