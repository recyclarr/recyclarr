using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaManagement;

namespace Recyclarr.Core.Tests.ServarrApi.MediaManagement;

internal sealed class MediaManagementDtoExtensionsTest
{
    [Test]
    public void GetDifferences_returns_diff_when_value_changes()
    {
        var oldDto = new MediaManagementDto
        {
            DownloadPropersAndRepacks = PropersAndRepacksMode.PreferAndUpgrade,
        };
        var newDto = new MediaManagementDto
        {
            DownloadPropersAndRepacks = PropersAndRepacksMode.DoNotUpgrade,
        };

        var differences = oldDto.GetDifferences(newDto);

        differences.Should().HaveCount(1);
        differences.Should().Contain(d => d.Contains("DownloadPropersAndRepacks"));
        differences.Should().Contain(d => d.Contains("PreferAndUpgrade"));
        differences.Should().Contain(d => d.Contains("DoNotUpgrade"));
    }

    [Test]
    public void GetDifferences_returns_empty_when_values_match()
    {
        var oldDto = new MediaManagementDto
        {
            DownloadPropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
        };
        var newDto = new MediaManagementDto
        {
            DownloadPropersAndRepacks = PropersAndRepacksMode.DoNotPrefer,
        };

        var differences = oldDto.GetDifferences(newDto);

        differences.Should().BeEmpty();
    }

    [Test]
    public void GetDifferences_returns_diff_when_old_is_null_and_new_is_set()
    {
        var oldDto = new MediaManagementDto { DownloadPropersAndRepacks = null };
        var newDto = new MediaManagementDto
        {
            DownloadPropersAndRepacks = PropersAndRepacksMode.DoNotUpgrade,
        };

        var differences = oldDto.GetDifferences(newDto);

        differences.Should().HaveCount(1);
    }

    [Test]
    public void GetDifferences_returns_empty_when_both_null()
    {
        var oldDto = new MediaManagementDto { DownloadPropersAndRepacks = null };
        var newDto = new MediaManagementDto { DownloadPropersAndRepacks = null };

        var differences = oldDto.GetDifferences(newDto);

        differences.Should().BeEmpty();
    }
}
