using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.SyncState;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class CustomFormatTransactionPhaseTest : CliIntegrationFixture
{
    private static PipelinePlan CreatePlan(params CustomFormatResource[] cfs)
    {
        var plan = new PipelinePlan();
        foreach (var cf in cfs)
        {
            plan.AddCustomFormat(new PlannedCustomFormat(cf));
        }

        return plan;
    }

    [Test]
    public async Task Create_new_cf_when_no_cache_and_no_name_match()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(),
            ApiFetchOutput = [],
            Plan = CreatePlan(NewCf.Data("one", "cf1")),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData { NewCustomFormats = { NewCf.Data("one", "cf1") } }
            );
    }

    [Test]
    public async Task Conflict_when_no_cache_and_single_name_match()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = NewCf.Data("one", "cf1");

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(),
            ApiFetchOutput = [new CustomFormatResource { Name = "one", Id = 5 }],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData
                {
                    ConflictingCustomFormats = { new ConflictingCustomFormat(guideCf, 5) },
                }
            );
    }

    [Test]
    public async Task Ambiguous_when_no_cache_and_multiple_name_matches()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = NewCf.Data("HULU", "cf1");

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(),
            ApiFetchOutput =
            [
                new CustomFormatResource { Name = "HULU", Id = 10 },
                new CustomFormatResource { Name = "Hulu", Id = 20 },
            ],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData
                {
                    AmbiguousCustomFormats =
                    {
                        new AmbiguousMatch("HULU", [("HULU", 10), ("Hulu", 20)]),
                    },
                }
            );
    }

    [Test]
    public async Task Update_cf_by_cached_id_regardless_of_name()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "guide-name",
            TrashId = "cf1",
            IncludeCustomFormatWhenRenaming = true,
        };

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf1", "old-name", 2)),
            ApiFetchOutput = [new CustomFormatResource { Name = "service-name", Id = 2 }],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData
                {
                    UpdatedCustomFormats =
                    {
                        NewCf.Data("guide-name", "cf1", 2) with
                        {
                            IncludeCustomFormatWhenRenaming = true,
                        },
                    },
                }
            );
    }

    [Test]
    public async Task Unchanged_cf_when_cached_id_matches_and_content_same()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = NewCf.Data("one", "cf1");

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf1", "one", 1)),
            ApiFetchOutput = [new CustomFormatResource { Name = "one", Id = 1 }],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData { UnchangedCustomFormats = { guideCf } }
            );
    }

    [Test]
    public async Task Create_new_cf_when_stale_cache_and_no_name_collision()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = NewCf.Data("two", "cf2");

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf2", "two", 200)), // ID 200 doesn't exist
            ApiFetchOutput = [], // Empty service
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(new CustomFormatTransactionData { NewCustomFormats = { guideCf } });
    }

    [Test]
    public async Task Conflict_when_stale_cache_and_name_collision()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = NewCf.Data("existing", "cf1");

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf1", "existing", 999)), // ID 999 deleted
            ApiFetchOutput = [new CustomFormatResource { Name = "existing", Id = 5 }], // Different CF
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData
                {
                    ConflictingCustomFormats = { new ConflictingCustomFormat(guideCf, 5) },
                }
            );
    }

    [Test]
    public async Task Deleted_cfs_populated_for_orphaned_cache_entries()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf2", "two", 2)),
            ApiFetchOutput = [new CustomFormatResource { Name = "two", Id = 2 }],
            Plan = new PipelinePlan(), // Empty plan - no CFs in config
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData
                {
                    DeletedCustomFormats = { new TrashIdMapping("cf2", "two", 2) },
                }
            );
    }

    [Test]
    public async Task Deleted_cfs_excludes_cfs_in_config()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf2", "two", 2)),
            ApiFetchOutput = [new CustomFormatResource { Name = "two", Id = 2 }],
            Plan = CreatePlan(NewCf.Data("two", "cf2")),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.DeletedCustomFormats.Should().BeEmpty();
    }

    [Test]
    public async Task Unchanged_when_specifications_match()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var specs = new CustomFormatSpecificationData[]
        {
            new()
            {
                Name = "TestSpec",
                Implementation = "ReleaseTitleSpecification",
                Negate = false,
                Required = true,
                Fields = [new CustomFormatFieldData { Name = "value", Value = "test" }],
            },
        };

        var guideCf = new CustomFormatResource
        {
            Name = "Test CF",
            TrashId = "cf1",
            Specifications = specs,
        };

        var serviceCf = new CustomFormatResource
        {
            Name = "Test CF",
            Id = 1,
            Specifications = specs,
        };

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf1", "Test CF", 1)),
            ApiFetchOutput = [serviceCf],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.UnchangedCustomFormats.Should().ContainSingle();
        context.TransactionOutput.UpdatedCustomFormats.Should().BeEmpty();
    }

    [Test]
    public async Task Updated_when_specification_differs()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "Test CF",
            TrashId = "cf1",
            Specifications =
            [
                new CustomFormatSpecificationData
                {
                    Name = "TestSpec",
                    Implementation = "ReleaseTitleSpecification",
                    Fields = [new CustomFormatFieldData { Name = "value", Value = "new-value" }],
                },
            ],
        };

        var serviceCf = new CustomFormatResource
        {
            Name = "Test CF",
            Id = 1,
            Specifications =
            [
                new CustomFormatSpecificationData
                {
                    Name = "TestSpec",
                    Implementation = "ReleaseTitleSpecification",
                    Fields = [new CustomFormatFieldData { Name = "value", Value = "old-value" }],
                },
            ],
        };

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf1", "Test CF", 1)),
            ApiFetchOutput = [serviceCf],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.UpdatedCustomFormats.Should().ContainSingle();
        context.TransactionOutput.UnchangedCustomFormats.Should().BeEmpty();
    }

    [Test]
    public async Task Updated_when_specification_count_differs()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "Test CF",
            TrashId = "cf1",
            Specifications =
            [
                new CustomFormatSpecificationData { Name = "Spec1" },
                new CustomFormatSpecificationData { Name = "Spec2" },
            ],
        };

        var serviceCf = new CustomFormatResource
        {
            Name = "Test CF",
            Id = 1,
            Specifications = [new CustomFormatSpecificationData { Name = "Spec1" }],
        };

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf1", "Test CF", 1)),
            ApiFetchOutput = [serviceCf],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.UpdatedCustomFormats.Should().ContainSingle();
    }

    [Test]
    public async Task Unchanged_when_spec_order_differs()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "Test CF",
            TrashId = "cf1",
            Specifications =
            [
                new CustomFormatSpecificationData { Name = "Spec1", Implementation = "Impl1" },
                new CustomFormatSpecificationData { Name = "Spec2", Implementation = "Impl2" },
            ],
        };

        var serviceCf = new CustomFormatResource
        {
            Name = "Test CF",
            Id = 1,
            Specifications =
            [
                new CustomFormatSpecificationData { Name = "Spec2", Implementation = "Impl2" },
                new CustomFormatSpecificationData { Name = "Spec1", Implementation = "Impl1" },
            ],
        };

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf1", "Test CF", 1)),
            ApiFetchOutput = [serviceCf],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.UnchangedCustomFormats.Should().ContainSingle();
        context.TransactionOutput.UpdatedCustomFormats.Should().BeEmpty();
    }

    [Test]
    public async Task Unchanged_when_service_has_extra_fields()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "Test CF",
            TrashId = "cf1",
            Specifications =
            [
                new CustomFormatSpecificationData
                {
                    Name = "TestSpec",
                    Implementation = "ReleaseTitleSpecification",
                    Fields = [new CustomFormatFieldData { Name = "value", Value = "test" }],
                },
            ],
        };

        var serviceCf = new CustomFormatResource
        {
            Name = "Test CF",
            Id = 1,
            Specifications =
            [
                new CustomFormatSpecificationData
                {
                    Name = "TestSpec",
                    Implementation = "ReleaseTitleSpecification",
                    Fields =
                    [
                        new CustomFormatFieldData { Name = "value", Value = "test" },
                        new CustomFormatFieldData { Name = "extraField", Value = "ignored" },
                    ],
                },
            ],
        };

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(new TrashIdMapping("cf1", "Test CF", 1)),
            ApiFetchOutput = [serviceCf],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.UnchangedCustomFormats.Should().ContainSingle();
        context.TransactionOutput.UpdatedCustomFormats.Should().BeEmpty();
    }

    [Test]
    public async Task Error_when_cache_has_duplicate_service_id_for_deletion_and_update()
    {
        // Scenario: Cache corruption results in two trash_ids mapping to the same service ID.
        // One is in config (would be unchanged), one is orphaned (would be deleted).
        // Expected: Sync detects the conflict and reports an error, telling user to run cache rebuild.
        // Sync should NOT silently delete a CF that's also being updated.
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                DeleteOldCustomFormats = true,
            }
        );

        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var context = new CustomFormatPipelineContext
        {
            State = CfCache.New(
                new TrashIdMapping("real-trash-id", "BR-DISK", 1), // In config
                new TrashIdMapping("orphan-trash-id", "BR-DISK", 1) // Orphaned, same service ID
            ),
            ApiFetchOutput = [new CustomFormatResource { Name = "BR-DISK", Id = 1 }],
            Plan = CreatePlan(NewCf.Data("BR-DISK", "real-trash-id")),
        };

        await sut.Execute(context, CancellationToken.None);

        // Sync should detect the conflict and NOT proceed with the conflicting deletion
        context.TransactionOutput.DeletedCustomFormats.Should().BeEmpty();
        // The valid CF should still be processed normally
        context.TransactionOutput.UnchangedCustomFormats.Should().ContainSingle();
        // Sync should report the cache inconsistency as an error
        context.TransactionOutput.InvalidCacheEntries.Should().ContainSingle();
    }
}
