using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Pipelines.QualityProfile.Api;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class QualityItemOrganizerTest
{
    private readonly QualityProfileConfig _config = new()
    {
        Qualities = new[]
        {
            NewQp.QualityConfig("one"),
            NewQp.QualityConfig("three"),
            NewQp.QualityConfig("six", false),
            NewQp.QualityConfig("seven"),
            NewQp.QualityConfig("nonexistent1"),
            NewQp.GroupConfig("group3", "eight"),
            NewQp.GroupConfig("group4", false, "nine", "ten"),
            NewQp.GroupConfig("group5", "eleven")
        }
    };

    private readonly QualityProfileDto _dto = new()
    {
        Items = new[]
        {
            NewQp.QualityDto(1, "one", true),
            NewQp.QualityDto(2, "two", true),
            NewQp.QualityDto(3, "three", true),
            NewQp.QualityDto(9, "nine", true),
            NewQp.GroupDto(50, "group5", true,
                NewQp.QualityDto(11, "eleven", true)),
            NewQp.QualityDto(10, "ten", true),
            NewQp.QualityDto(4, "four", true),
            NewQp.GroupDto(1001, "group1", true,
                NewQp.QualityDto(5, "five", true),
                NewQp.QualityDto(6, "six", true)),
            NewQp.GroupDto(1002, "group2", true,
                NewQp.QualityDto(7, "seven", true)),
            NewQp.QualityDto(8, "eight", true)
        }
    };

    [Test]
    public void Update_qualities_top_sort()
    {
        var sut = new QualityItemOrganizer();
        var result = sut.OrganizeItems(_dto, _config with
        {
            QualitySort = QualitySortAlgorithm.Top
        });

        result.Should().BeEquivalentTo(new UpdatedQualities
        {
            InvalidQualityNames = new[] {"nonexistent1"},
            NumWantedItems = 7,
            Items = new[]
            {
                // ------ IN CONFIG ------
                NewQp.QualityDto(1, "one", true),
                NewQp.QualityDto(3, "three", true),
                NewQp.QualityDto(6, "six", false),
                NewQp.QualityDto(7, "seven", true),
                NewQp.GroupDto(1002, "group3", true,
                    NewQp.QualityDto(8, "eight", true)),
                NewQp.GroupDto(1003, "group4", false,
                    NewQp.QualityDto(9, "nine", false),
                    NewQp.QualityDto(10, "ten", false)),
                NewQp.GroupDto(50, "group5", true,
                    NewQp.QualityDto(11, "eleven", true)),
                // ------ NOT IN CONFIG ------
                NewQp.QualityDto(2, "two", false),
                NewQp.QualityDto(4, "four", false),
                NewQp.GroupDto(1001, "group1", false,
                    NewQp.QualityDto(5, "five", false))
            }
        });
    }

    [Test]
    public void Update_qualities_bottom_sort()
    {
        var sut = new QualityItemOrganizer();
        var result = sut.OrganizeItems(_dto, _config with
        {
            QualitySort = QualitySortAlgorithm.Bottom
        });

        result.Should().BeEquivalentTo(new UpdatedQualities
        {
            InvalidQualityNames = new[] {"nonexistent1"},
            NumWantedItems = 7,
            Items = new[]
            {
                // ------ NOT IN CONFIG ------
                NewQp.QualityDto(2, "two", false),
                NewQp.QualityDto(4, "four", false),
                NewQp.GroupDto(1001, "group1", false,
                    NewQp.QualityDto(5, "five", false)),
                // ------ IN CONFIG ------
                NewQp.QualityDto(1, "one", true),
                NewQp.QualityDto(3, "three", true),
                NewQp.QualityDto(6, "six", false),
                NewQp.QualityDto(7, "seven", true),
                NewQp.GroupDto(1002, "group3", true,
                    NewQp.QualityDto(8, "eight", true)),
                NewQp.GroupDto(1003, "group4", false,
                    NewQp.QualityDto(9, "nine", false),
                    NewQp.QualityDto(10, "ten", false)),
                NewQp.GroupDto(50, "group5", true,
                    NewQp.QualityDto(11, "eleven", true))
            }
        });
    }
}
