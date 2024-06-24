using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Tests.TestLibrary;

namespace Recyclarr.Cli.Tests.Cache;

[TestFixture]
public class CustomFormatCacheTest
{
    [Test]
    public void New_updated_and_changed_are_added()
    {
        var transactions = new CustomFormatTransactionData
        {
            NewCustomFormats =
            {
                NewCf.Data("one", "1", 1),
                NewCf.Data("two", "2", 2)
            },
            UpdatedCustomFormats =
            {
                NewCf.Data("three", "3", 3)
            },
            UnchangedCustomFormats =
            {
                NewCf.Data("four", "4", 4)
            }
        };

        var cache = new CustomFormatCache([]);
        cache.Update(transactions);

        cache.Mappings.Should().BeEquivalentTo(new[]
        {
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("2", "two", 2),
            new CfTrashIdMapping("3", "three", 3),
            new CfTrashIdMapping("4", "four", 4)
        });
    }

    [Test]
    public void Deleted_cfs_are_removed()
    {
        var transactions = new CustomFormatTransactionData
        {
            NewCustomFormats =
            {
                NewCf.Data("one", "1", 1),
                NewCf.Data("two", "2", 2)
            },
            DeletedCustomFormats =
            {
                new CfTrashIdMapping("3", "three", 3)
            }
        };

        var cache = new CustomFormatCache([
            new CfTrashIdMapping("3", "three", 3),
            new CfTrashIdMapping("4", "four", 4)
        ]);

        cache.Update(transactions);

        cache.Mappings.Should().BeEquivalentTo(new[]
        {
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("2", "two", 2),
            new CfTrashIdMapping("4", "four", 4)
        });
    }

    [Test]
    public void Cfs_not_in_service_are_removed()
    {
        var serviceCfs = new[]
        {
            NewCf.Data("one", "1", 1),
            NewCf.Data("two", "2", 2)
        };

        var cache = new CustomFormatCache([
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("2", "two", 2),
            new CfTrashIdMapping("3", "three", 3),
            new CfTrashIdMapping("4", "four", 4)
        ]);

        cache.RemoveStale(serviceCfs);

        cache.Mappings.Should().BeEquivalentTo(new[]
        {
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("2", "two", 2)
        });
    }

    [Test]
    public void Cache_update_skips_custom_formats_with_zero_id()
    {
        var transactions = new CustomFormatTransactionData
        {
            NewCustomFormats =
            {
                NewCf.Data("one", "1", 1),
                NewCf.Data("zero", "0")
            },
            UpdatedCustomFormats =
            {
                NewCf.Data("two", "2", 2)
            }
        };

        var cache = new CustomFormatCache([]);

        cache.Update(transactions);

        cache.Mappings.Should().BeEquivalentTo(new[]
        {
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("2", "two", 2)
        });
    }

    [Test]
    public void Existing_matching_mappings_should_be_replaced()
    {
        var transactions = new CustomFormatTransactionData
        {
            NewCustomFormats =
            {
                NewCf.Data("one_new", "1", 1),
                NewCf.Data("two_new", "2", 2)
            },
            UpdatedCustomFormats =
            {
                NewCf.Data("three_new", "3", 3)
            },
            UnchangedCustomFormats =
            {
                NewCf.Data("four_new", "4", 4)
            }
        };

        var cache = new CustomFormatCache([
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("2", "two", 2),
            new CfTrashIdMapping("3", "three", 3),
            new CfTrashIdMapping("4", "four", 4)
        ]);

        cache.Update(transactions);

        cache.Mappings.Should().BeEquivalentTo(new[]
        {
            new CfTrashIdMapping("1", "one_new", 1),
            new CfTrashIdMapping("2", "two_new", 2),
            new CfTrashIdMapping("3", "three_new", 3),
            new CfTrashIdMapping("4", "four_new", 4)
        });
    }

    [Test]
    public void Duplicate_mappings_should_be_removed()
    {
        var transactions = new CustomFormatTransactionData();

        var cache = new CustomFormatCache([
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("12", "one2", 1),
            new CfTrashIdMapping("2", "two", 2),
            new CfTrashIdMapping("3", "three", 3),
            new CfTrashIdMapping("4", "four", 4)
        ]);

        cache.Update(transactions);

        cache.Mappings.Should().BeEquivalentTo(new[]
        {
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("2", "two", 2),
            new CfTrashIdMapping("3", "three", 3),
            new CfTrashIdMapping("4", "four", 4)
        });
    }

    [Test]
    public void Mappings_ordered_by_id()
    {
        var transactions = new CustomFormatTransactionData();

        var cache = new CustomFormatCache([
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("3", "three", 3),
            new CfTrashIdMapping("4", "four", 4),
            new CfTrashIdMapping("2", "two", 2)
        ]);

        cache.Update(transactions);

        cache.Mappings.Should().BeEquivalentTo(new[]
        {
            new CfTrashIdMapping("1", "one", 1),
            new CfTrashIdMapping("2", "two", 2),
            new CfTrashIdMapping("3", "three", 3),
            new CfTrashIdMapping("4", "four", 4)
        }, o => o.WithStrictOrdering());
    }
}
