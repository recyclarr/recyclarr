using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Cli.Tests.Cache;

internal sealed class CustomFormatCacheTest
{
    [Test]
    public void New_updated_and_changed_are_added()
    {
        var transactions = new CustomFormatTransactionData
        {
            NewCustomFormats = { NewCf.Data("one", "1", 1), NewCf.Data("two", "2", 2) },
            UpdatedCustomFormats = { NewCf.Data("three", "3", 3) },
            UnchangedCustomFormats = { NewCf.Data("four", "4", 4) },
        };

        var serviceCfs = new[]
        {
            NewCf.Data("one", "1", 1),
            NewCf.Data("two", "2", 2),
            NewCf.Data("three", "3", 3),
            NewCf.Data("four", "4", 4),
        };

        var cache = CfCache.New();
        cache.Update(transactions, serviceCfs);

        cache
            .Mappings.Should()
            .BeEquivalentTo([
                new TrashIdMapping("1", "one", 1),
                new TrashIdMapping("2", "two", 2),
                new TrashIdMapping("3", "three", 3),
                new TrashIdMapping("4", "four", 4),
            ]);
    }

    [Test]
    public void Deleted_cfs_are_removed()
    {
        var transactions = new CustomFormatTransactionData
        {
            NewCustomFormats = { NewCf.Data("one", "1", 1), NewCf.Data("two", "2", 2) },
            DeletedCustomFormats = { new TrashIdMapping("3", "three", 3) },
        };

        // Note: ID 3 is being deleted, so it's not in serviceCfs
        var serviceCfs = new[]
        {
            NewCf.Data("one", "1", 1),
            NewCf.Data("two", "2", 2),
            NewCf.Data("four", "4", 4),
        };

        var cache = CfCache.New(
            new TrashIdMapping("3", "three", 3),
            new TrashIdMapping("4", "four", 4)
        );

        cache.Update(transactions, serviceCfs);

        cache
            .Mappings.Should()
            .BeEquivalentTo([
                new TrashIdMapping("1", "one", 1),
                new TrashIdMapping("2", "two", 2),
                new TrashIdMapping("4", "four", 4),
            ]);
    }

    [Test]
    public void Cfs_not_in_service_are_removed()
    {
        var serviceCfs = new[] { NewCf.Data("one", "1", 1), NewCf.Data("two", "2", 2) };

        var cache = CfCache.New(
            new TrashIdMapping("1", "one", 1),
            new TrashIdMapping("2", "two", 2),
            new TrashIdMapping("3", "three", 3),
            new TrashIdMapping("4", "four", 4)
        );

        cache.Update(new CustomFormatTransactionData(), serviceCfs);

        cache
            .Mappings.Should()
            .BeEquivalentTo([new TrashIdMapping("1", "one", 1), new TrashIdMapping("2", "two", 2)]);
    }

    [Test]
    public void Cache_update_skips_custom_formats_with_zero_id()
    {
        var transactions = new CustomFormatTransactionData
        {
            NewCustomFormats = { NewCf.Data("one", "1", 1), NewCf.Data("zero", "0") },
            UpdatedCustomFormats = { NewCf.Data("two", "2", 2) },
        };

        var serviceCfs = new[] { NewCf.Data("one", "1", 1), NewCf.Data("two", "2", 2) };

        var cache = CfCache.New();

        cache.Update(transactions, serviceCfs);

        cache
            .Mappings.Should()
            .BeEquivalentTo([new TrashIdMapping("1", "one", 1), new TrashIdMapping("2", "two", 2)]);
    }

    [Test]
    public void Existing_matching_mappings_should_be_replaced()
    {
        var transactions = new CustomFormatTransactionData
        {
            NewCustomFormats = { NewCf.Data("one_new", "1", 1), NewCf.Data("two_new", "2", 2) },
            UpdatedCustomFormats = { NewCf.Data("three_new", "3", 3) },
            UnchangedCustomFormats = { NewCf.Data("four_new", "4", 4) },
        };

        var serviceCfs = new[]
        {
            NewCf.Data("one_new", "1", 1),
            NewCf.Data("two_new", "2", 2),
            NewCf.Data("three_new", "3", 3),
            NewCf.Data("four_new", "4", 4),
        };

        var cache = CfCache.New(
            new TrashIdMapping("1", "one", 1),
            new TrashIdMapping("2", "two", 2),
            new TrashIdMapping("3", "three", 3),
            new TrashIdMapping("4", "four", 4)
        );

        cache.Update(transactions, serviceCfs);

        cache
            .Mappings.Should()
            .BeEquivalentTo([
                new TrashIdMapping("1", "one_new", 1),
                new TrashIdMapping("2", "two_new", 2),
                new TrashIdMapping("3", "three_new", 3),
                new TrashIdMapping("4", "four_new", 4),
            ]);
    }

    [Test]
    public void Duplicate_mappings_should_be_removed()
    {
        var transactions = new CustomFormatTransactionData();

        var serviceCfs = new[]
        {
            NewCf.Data("one", "1", 1),
            NewCf.Data("two", "2", 2),
            NewCf.Data("three", "3", 3),
            NewCf.Data("four", "4", 4),
        };

        var cache = CfCache.New(
            new TrashIdMapping("1", "one", 1),
            new TrashIdMapping("12", "one2", 1),
            new TrashIdMapping("2", "two", 2),
            new TrashIdMapping("3", "three", 3),
            new TrashIdMapping("4", "four", 4)
        );

        cache.Update(transactions, serviceCfs);

        cache
            .Mappings.Should()
            .BeEquivalentTo([
                new TrashIdMapping("1", "one", 1),
                new TrashIdMapping("2", "two", 2),
                new TrashIdMapping("3", "three", 3),
                new TrashIdMapping("4", "four", 4),
            ]);
    }

    [Test]
    public void Mappings_ordered_by_id()
    {
        var transactions = new CustomFormatTransactionData();

        var serviceCfs = new[]
        {
            NewCf.Data("one", "1", 1),
            NewCf.Data("two", "2", 2),
            NewCf.Data("three", "3", 3),
            NewCf.Data("four", "4", 4),
        };

        var cache = CfCache.New(
            new TrashIdMapping("1", "one", 1),
            new TrashIdMapping("3", "three", 3),
            new TrashIdMapping("4", "four", 4),
            new TrashIdMapping("2", "two", 2)
        );

        cache.Update(transactions, serviceCfs);

        cache
            .Mappings.Should()
            .BeEquivalentTo(
                [
                    new TrashIdMapping("1", "one", 1),
                    new TrashIdMapping("2", "two", 2),
                    new TrashIdMapping("3", "three", 3),
                    new TrashIdMapping("4", "four", 4),
                ],
                o => o.WithStrictOrdering()
            );
    }

    [Test]
    public void Mappings_with_duplicate_ids_are_removed()
    {
        // Arrange: Cache has two mappings with same CustomFormatId
        var cache = CfCache.New(
            new TrashIdMapping("first-trash-id", "First Format", 3), // First occurrence - should be kept
            new TrashIdMapping("second-trash-id", "Second Format", 3), // Duplicate ID - should be removed
            new TrashIdMapping("other-trash-id", "Other Format", 5) // Different ID, should remain
        );

        // Service has CF with the duplicate ID
        var serviceCfs = new[]
        {
            NewCf.Data("Some Format", "service-trash-id", 3),
            NewCf.Data("Other Format", "other-service-trash-id", 5),
        };

        // Act
        cache.Update(new CustomFormatTransactionData(), serviceCfs);

        // Assert: Should have exactly one mapping for each unique CustomFormatId
        cache
            .Mappings.Should()
            .HaveCount(2)
            .And.ContainSingle(x => x.ServiceId == 3)
            .And.ContainSingle(x => x.ServiceId == 5);
    }
}
