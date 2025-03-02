using Recyclarr.Cli.Pipelines.QualityProfile;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ServarrApi.QualityProfile;

// ReSharper disable CollectionNeverUpdated.Local

namespace Recyclarr.Cli.Tests.Pipelines.QualityProfile;

internal sealed class QualityProfileExtensionsTest
{
    [Test]
    public void Find_group_by_id_with_null_input()
    {
        var dto = new List<ProfileItemDto>();

        var result = dto.FindGroupById(null);

        result.Should().BeNull();
    }

    [Test]
    public void Find_group_by_id_with_match()
    {
        var targetItem = NewQp.GroupDto(6, "Group 6", true);
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(1, "Group 1", true, NewQp.QualityDto(5, "Quality 5", true)),
            NewQp.GroupDto(2, "Group 2", true),
            targetItem,
            NewQp.GroupDto(3, "Group 3", true),
        };

        var result = dto.FindGroupById(6);

        result.Should().Be(targetItem);
    }

    [Test]
    public void Find_group_by_id_with_nested_match()
    {
        var targetItem = NewQp.GroupDto(6, "Quality Item 6", true);
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(1, "Group 1", true, NewQp.QualityDto(5, "Quality 5", true)),
            NewQp.GroupDto(2, "Group 2", true, targetItem),
            NewQp.GroupDto(3, "Group 3", true),
        };

        var result = dto.FindGroupById(6);

        result.Should().Be(targetItem);
    }

    [Test]
    public void Find_group_by_id_with_no_items()
    {
        var dto = new List<ProfileItemDto>();

        var result = dto.FindGroupById(5);

        result.Should().BeNull();
    }

    [Test]
    public void Find_group_by_id_with_no_match()
    {
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.QualityDto(5, "Quality 5", true),
            NewQp.GroupDto(6, "Group 6", true),
        };

        var result = dto.FindGroupById(5);

        result.Should().BeNull();
    }

    [Test]
    public void Find_group_by_name_with_null_input()
    {
        var dto = new List<ProfileItemDto>();

        var result = dto.FindGroupByName(null);

        result.Should().BeNull();
    }

    [Test]
    public void Find_group_by_name_with_case_insensitive_match()
    {
        var targetItem = NewQp.GroupDto(6, "Group 6", true);
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(1, "Group 1", true, NewQp.QualityDto(5, "Quality 5", true)),
            NewQp.GroupDto(2, "Group 2", true),
            targetItem,
            NewQp.GroupDto(3, "Group 3", true),
        };

        var result = dto.FindGroupByName("grOUp 6");

        result.Should().Be(targetItem);
    }

    [Test]
    public void Find_group_by_name_with_nested_match()
    {
        var targetItem = NewQp.GroupDto(6, "Group 6", true);
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(1, "Group 1", true, NewQp.QualityDto(5, "Quality 5", true)),
            NewQp.GroupDto(2, "Group 2", true, targetItem),
            NewQp.GroupDto(3, "Group 3", true),
        };

        var result = dto.FindGroupByName("Group 6");

        result.Should().Be(targetItem);
    }

    [Test]
    public void Find_group_by_name_with_no_items()
    {
        var dto = new List<ProfileItemDto>();

        var result = dto.FindGroupByName("Group 5");

        result.Should().BeNull();
    }

    [Test]
    public void Find_group_by_name_with_no_match()
    {
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.QualityDto(5, "Group 5", true),
            NewQp.GroupDto(6, "Group 6", true),
        };

        var result = dto.FindGroupByName("Group 5");

        result.Should().BeNull();
    }

    [Test]
    public void Find_quality_by_id_with_null_input()
    {
        var dto = new List<ProfileItemDto>();

        var result = dto.FindQualityById(null);

        result.Should().BeNull();
    }

    [Test]
    public void Find_quality_by_id_with_match()
    {
        var targetItem = NewQp.QualityDto(6, "Quality 6", true);
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(1, "Group 1", true, NewQp.QualityDto(5, "Quality 5", true)),
            NewQp.GroupDto(2, "Group 2", true),
            targetItem,
            NewQp.GroupDto(3, "Group 3", true),
        };

        var result = dto.FindQualityById(6);

        result.Should().Be(targetItem);
    }

    [Test]
    public void Find_quality_by_id_with_nested_match()
    {
        var targetItem = NewQp.QualityDto(6, "Quality 6", true);
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(1, "Group 1", true, NewQp.QualityDto(5, "Quality 5", true)),
            NewQp.GroupDto(2, "Group 2", true, targetItem),
            NewQp.GroupDto(3, "Group 3", true),
        };

        var result = dto.FindQualityById(6);

        result.Should().Be(targetItem);
    }

    [Test]
    public void Find_quality_by_id_with_no_items()
    {
        var dto = new List<ProfileItemDto>();

        var result = dto.FindQualityById(5);

        result.Should().BeNull();
    }

    [Test]
    public void Find_quality_by_id_with_no_match()
    {
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(5, "Group 5", true),
            NewQp.GroupDto(6, "Group 6", true),
        };

        var result = dto.FindQualityById(5);

        result.Should().BeNull();
    }

    //----------------------------------------------------------------------------

    [Test]
    public void Find_quality_by_name_with_null_input()
    {
        var dto = new List<ProfileItemDto>();

        var result = dto.FindQualityByName(null);

        result.Should().BeNull();
    }

    [Test]
    public void Find_quality_by_name_with_case_insensitive_match()
    {
        var targetItem = NewQp.QualityDto(6, "Quality 6", true);
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(1, "Group 1", true, NewQp.QualityDto(5, "Quality 5", true)),
            NewQp.GroupDto(2, "Group 2", true),
            targetItem,
            NewQp.GroupDto(3, "Group 3", true),
        };

        var result = dto.FindQualityByName("quALIty 6");

        result.Should().Be(targetItem);
    }

    [Test]
    public void Find_quality_by_name_with_nested_match()
    {
        var targetItem = NewQp.QualityDto(6, "Quality 6", true);
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(1, "Group 1", true, NewQp.QualityDto(5, "Quality 5", true)),
            NewQp.GroupDto(2, "Group 2", true, targetItem),
            NewQp.GroupDto(3, "Group 3", true),
        };

        var result = dto.FindQualityByName("Quality 6");

        result.Should().Be(targetItem);
    }

    [Test]
    public void Find_quality_by_name_with_no_items()
    {
        var dto = new List<ProfileItemDto>();

        var result = dto.FindQualityByName("Quality 5");

        result.Should().BeNull();
    }

    [Test]
    public void Find_quality_by_name_with_no_match()
    {
        var dto = new List<ProfileItemDto>
        {
            NewQp.QualityDto(4, "Quality 4", true),
            NewQp.GroupDto(5, "Quality 5", true),
            NewQp.GroupDto(6, "Group 6", true),
        };

        var result = dto.FindQualityByName("Quality 5");

        result.Should().BeNull();
    }

    [Test]
    public void Create_new_item_id_with_no_items()
    {
        var dto = new QualityProfileDto { Items = new List<ProfileItemDto>() };

        var result = dto.Items.NewItemId();

        result.Should().Be(1001);
    }

    [Test]
    public void Create_new_item_id_with_items_below_1000()
    {
        var dto = new List<ProfileItemDto>
        {
            NewQp.GroupDto(1, "Group 1", true),
            NewQp.QualityDto(2, "Quality 2", true),
            NewQp.GroupDto(
                3,
                "Group 3",
                true,
                NewQp.GroupDto(6, "Group 6", true),
                NewQp.QualityDto(7, "Quality 7", true)
            ),
            NewQp.GroupDto(4, "Group 4", true, NewQp.QualityDto(5, "Quality 5", true)),
        };

        var result = dto.NewItemId();

        result.Should().Be(1001);
    }

    [Test]
    public void Create_new_item_id_with_leaf_items_above_1000()
    {
        var dto = new List<ProfileItemDto>
        {
            NewQp.GroupDto(1, "Group 1", true),
            NewQp.QualityDto(2, "Quality 2", true),
            NewQp.GroupDto(
                3,
                "Group 3",
                true,
                NewQp.GroupDto(1006, "Group 6", true),
                NewQp.QualityDto(1007, "Quality 7", true)
            ),
            NewQp.GroupDto(4, "Group 4", true, NewQp.QualityDto(5, "Quality 5", true)),
        };

        var result = dto.NewItemId();

        result.Should().Be(1007);
    }

    [Test]
    public void Create_new_item_id_with_parent_items_above_1000()
    {
        var dto = new List<ProfileItemDto>
        {
            NewQp.GroupDto(1, "Group 1", true),
            NewQp.QualityDto(2, "Quality 2", true),
            NewQp.GroupDto(
                1008,
                "Group 3",
                true,
                NewQp.GroupDto(1006, "Group 6", true),
                NewQp.QualityDto(1007, "Quality 7", true)
            ),
            NewQp.GroupDto(4, "Group 4", true, NewQp.QualityDto(5, "Quality 5", true)),
        };

        var result = dto.NewItemId();

        result.Should().Be(1009);
    }

    [Test]
    public void Reverse_items_works()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.ReverseItems();

        result
            .Items.Should()
            .BeEquivalentTo(
                [
                    NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
                    NewQp.GroupDto(
                        1002,
                        "Group 2",
                        true,
                        NewQp.QualityDto(3, "Quality 3", true),
                        NewQp.QualityDto(2, "Quality 2", true)
                    ),
                    NewQp.QualityDto(1, "Quality 1", true),
                    NewQp.GroupDto(1001, "Group 1", true),
                ]
            );
    }

    [Test]
    public void Find_cutoff_id_with_group_name()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.Items.FindCutoff("Group 2");

        result.Should().Be(1002);
    }

    [Test]
    public void Find_cutoff_id_with_quality_name()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.Items.FindCutoff("Quality 1");

        result.Should().Be(1);
    }

    [Test]
    public void Find_cutoff_id_with_nested_quality_name()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.Items.FindCutoff("Quality 2");

        result.Should().BeNull();
    }

    [Test]
    public void Find_cutoff_id_with_null_name()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.Items.FindCutoff((string?)null);

        result.Should().BeNull();
    }

    [Test]
    public void Find_cutoff_name_with_group_id()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.Items.FindCutoff(1002);

        result.Should().Be("Group 2");
    }

    [Test]
    public void Find_cutoff_name_with_quality_id()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.Items.FindCutoff(1);

        result.Should().Be("Quality 1");
    }

    [Test]
    public void Find_cutoff_name_with_nested_quality_id()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.Items.FindCutoff(2);

        result.Should().BeNull();
    }

    [Test]
    public void Find_cutoff_name_with_null_id()
    {
        var dto = new QualityProfileDto
        {
            Items = new List<ProfileItemDto>
            {
                NewQp.GroupDto(1001, "Group 1", true),
                NewQp.QualityDto(1, "Quality 1", true),
                NewQp.GroupDto(
                    1002,
                    "Group 2",
                    true,
                    NewQp.QualityDto(2, "Quality 2", true),
                    NewQp.QualityDto(3, "Quality 3", true)
                ),
                NewQp.GroupDto(1003, "Group 3", true, NewQp.QualityDto(4, "Quality 4", true)),
            },
        };

        var result = dto.Items.FindCutoff((int?)null);

        result.Should().BeNull();
    }
}
