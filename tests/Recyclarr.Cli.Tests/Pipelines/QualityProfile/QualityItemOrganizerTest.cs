using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Models;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

internal sealed class QualityItemOrganizerTest
{
    private readonly QualityProfileConfig _config = new()
    {
        Qualities =
        [
            NewQp.QualityConfig("one"),
            NewQp.QualityConfig("three"),
            NewQp.QualityConfig("six", false),
            NewQp.QualityConfig("seven"),
            NewQp.QualityConfig("nonexistent1"),
            NewQp.GroupConfig("group3", "eight"),
            NewQp.GroupConfig("group4", false, "nine", "ten"),
            NewQp.GroupConfig("group5", "eleven"),
        ],
    };

    private readonly IReadOnlyCollection<QualityProfileItem> _items =
    [
        NewQp.QualityItem(1, "one", true),
        NewQp.QualityItem(2, "two", true),
        NewQp.QualityItem(3, "three", true),
        NewQp.QualityItem(9, "nine", true),
        NewQp.GroupItem(50, "group5", true, NewQp.QualityItem(11, "eleven", true)),
        NewQp.QualityItem(10, "ten", true),
        NewQp.QualityItem(4, "four", true),
        NewQp.GroupItem(
            1001,
            "group1",
            true,
            NewQp.QualityItem(5, "five", true),
            NewQp.QualityItem(6, "six", true)
        ),
        NewQp.GroupItem(1002, "group2", true, NewQp.QualityItem(7, "seven", true)),
        NewQp.QualityItem(8, "eight", true),
    ];

    [Test]
    public void Update_qualities_top_sort()
    {
        var sut = new QualityItemOrganizer();
        var result = sut.OrganizeItems(
            _items,
            _config with
            {
                QualitySort = QualitySortAlgorithm.Top,
            }
        );

        result
            .Should()
            .BeEquivalentTo(
                new UpdatedQualities
                {
                    InvalidQualityNames = ["nonexistent1"],
                    NumWantedItems = 7,
                    Items =
                    [
                        // ------ IN CONFIG ------
                        NewQp.QualityItem(1, "one", true),
                        NewQp.QualityItem(3, "three", true),
                        NewQp.QualityItem(6, "six", false),
                        NewQp.QualityItem(7, "seven", true),
                        NewQp.GroupItem(1001, "group3", true, NewQp.QualityItem(8, "eight", true)),
                        NewQp.GroupItem(
                            1002,
                            "group4",
                            false,
                            NewQp.QualityItem(9, "nine", false),
                            NewQp.QualityItem(10, "ten", false)
                        ),
                        NewQp.GroupItem(50, "group5", true, NewQp.QualityItem(11, "eleven", true)),
                        // ------ NOT IN CONFIG ------
                        NewQp.QualityItem(2, "two", false),
                        NewQp.QualityItem(4, "four", false),
                        NewQp.QualityItem(5, "five", false),
                    ],
                }
            );
    }

    [Test]
    public void Update_qualities_bottom_sort()
    {
        var sut = new QualityItemOrganizer();
        var result = sut.OrganizeItems(
            _items,
            _config with
            {
                QualitySort = QualitySortAlgorithm.Bottom,
            }
        );

        result
            .Should()
            .BeEquivalentTo(
                new UpdatedQualities
                {
                    InvalidQualityNames = ["nonexistent1"],
                    NumWantedItems = 7,
                    Items =
                    [
                        // ------ NOT IN CONFIG ------
                        NewQp.QualityItem(2, "two", false),
                        NewQp.QualityItem(4, "four", false),
                        NewQp.QualityItem(5, "five", false),
                        // ------ IN CONFIG ------
                        NewQp.QualityItem(1, "one", true),
                        NewQp.QualityItem(3, "three", true),
                        NewQp.QualityItem(6, "six", false),
                        NewQp.QualityItem(7, "seven", true),
                        NewQp.GroupItem(1001, "group3", true, NewQp.QualityItem(8, "eight", true)),
                        NewQp.GroupItem(
                            1002,
                            "group4",
                            false,
                            NewQp.QualityItem(9, "nine", false),
                            NewQp.QualityItem(10, "ten", false)
                        ),
                        NewQp.GroupItem(50, "group5", true, NewQp.QualityItem(11, "eleven", true)),
                    ],
                }
            );
    }

    [Test]
    public void Remove_empty_group()
    {
        var config = new QualityProfileConfig { Qualities = [NewQp.QualityConfig("one")] };

        IReadOnlyCollection<QualityProfileItem> items =
        [
            NewQp.GroupItem(1001, "group1", true, NewQp.QualityItem(1, "one", true)),
        ];

        var sut = new QualityItemOrganizer();
        var result = sut.OrganizeItems(items, config);

        result.Items.Should().BeEquivalentTo([NewQp.QualityItem(1, "one", true)]);
    }

    [Test]
    public void Flatten_group_with_one_remaining_item()
    {
        var config = new QualityProfileConfig { Qualities = [NewQp.QualityConfig("one")] };

        IReadOnlyCollection<QualityProfileItem> items =
        [
            NewQp.GroupItem(
                1001,
                "group1",
                true,
                NewQp.QualityItem(1, "one", true),
                NewQp.QualityItem(2, "two", true)
            ),
        ];

        var sut = new QualityItemOrganizer();
        var result = sut.OrganizeItems(items, config);

        result
            .Items.Should()
            .BeEquivalentTo([
                NewQp.QualityItem(1, "one", true),
                NewQp.QualityItem(2, "two", false),
            ]);
    }

    [Test]
    public void Do_not_flatten_group_with_two_remaining_items()
    {
        var config = new QualityProfileConfig { Qualities = [NewQp.QualityConfig("one")] };

        IReadOnlyCollection<QualityProfileItem> items =
        [
            NewQp.GroupItem(
                1001,
                "group1",
                true,
                NewQp.QualityItem(1, "one", true),
                NewQp.QualityItem(2, "two", true),
                NewQp.QualityItem(3, "three", true)
            ),
        ];

        var sut = new QualityItemOrganizer();
        var result = sut.OrganizeItems(items, config);

        result
            .Items.Should()
            .BeEquivalentTo([
                NewQp.QualityItem(1, "one", true),
                NewQp.GroupItem(
                    1001,
                    "group1",
                    false,
                    NewQp.QualityItem(2, "two", false),
                    NewQp.QualityItem(3, "three", false)
                ),
            ]);
    }

    [Test]
    public void Remove_quality_from_existing_group()
    {
        var config = new QualityProfileConfig
        {
            Qualities = [NewQp.GroupConfig("group1", "one", "two", "three")],
        };

        IReadOnlyCollection<QualityProfileItem> items =
        [
            NewQp.GroupItem(
                1001,
                "group1",
                true,
                NewQp.QualityItem(1, "one", true),
                NewQp.QualityItem(2, "two", true),
                NewQp.QualityItem(3, "three", true),
                NewQp.QualityItem(4, "four", true)
            ),
        ];

        var sut = new QualityItemOrganizer();
        var result = sut.OrganizeItems(items, config);

        result
            .Items.Should()
            .BeEquivalentTo([
                NewQp.GroupItem(
                    1001,
                    "group1",
                    true,
                    NewQp.QualityItem(1, "one", true),
                    NewQp.QualityItem(2, "two", true),
                    NewQp.QualityItem(3, "three", true)
                ),
                NewQp.QualityItem(4, "four", false),
            ]);
    }
}
