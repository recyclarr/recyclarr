using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Domain;
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
    public async Task Add_new_cf()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(),
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
    public async Task Update_cf_by_matching_name()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "one",
            TrashId = "cf1",
            IncludeCustomFormatWhenRenaming = true,
        };

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(),
            ApiFetchOutput = [new CustomFormatResource { Name = "one" }],
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
                        NewCf.Data("one", "cf1") with
                        {
                            IncludeCustomFormatWhenRenaming = true,
                        },
                    },
                }
            );
    }

    [Test]
    public async Task Update_cf_by_matching_id_different_names()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());
        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "different1",
            TrashId = "cf1",
            IncludeCustomFormatWhenRenaming = true,
        };

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(new TrashIdMapping("cf1", "", 2)),
            ApiFetchOutput = [new CustomFormatResource { Name = "different2", Id = 2 }],
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
                        NewCf.Data("different1", "cf1", 2) with
                        {
                            IncludeCustomFormatWhenRenaming = true,
                        },
                    },
                }
            );
    }

    [Test]
    public async Task Update_cf_by_matching_id_same_names()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = true,
            }
        );

        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "different1",
            TrashId = "cf1",
            IncludeCustomFormatWhenRenaming = true,
        };

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(),
            ApiFetchOutput = [new CustomFormatResource { Name = "different1", Id = 2 }],
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
                        NewCf.Data("different1", "cf1", 2) with
                        {
                            IncludeCustomFormatWhenRenaming = true,
                        },
                    },
                }
            );
    }

    [Test]
    public async Task Conflicting_cf_when_new_cf_has_name_of_existing()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = false,
            }
        );

        var sut = scope.Resolve<CustomFormatTransactionPhase>();
        var guideCf = NewCf.Data("one", "cf1");

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(),
            ApiFetchOutput =
            [
                new CustomFormatResource { Name = "one", Id = 2 },
                new CustomFormatResource { Name = "two", Id = 1 },
            ],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData
                {
                    ConflictingCustomFormats = { new ConflictingCustomFormat(guideCf, 2) },
                }
            );
    }

    [Test]
    public async Task Conflicting_cf_when_cached_cf_has_name_of_existing()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = false,
            }
        );

        var sut = scope.Resolve<CustomFormatTransactionPhase>();
        var guideCf = NewCf.Data("one", "cf1");

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(new TrashIdMapping("cf1", "one", 1)),
            ApiFetchOutput =
            [
                new CustomFormatResource { Name = "one", Id = 2 },
                new CustomFormatResource { Name = "two", Id = 1 },
            ],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData
                {
                    ConflictingCustomFormats = { new ConflictingCustomFormat(guideCf, 2) },
                }
            );
    }

    [Test]
    public async Task Updated_cf_with_matching_name_and_id()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = false,
            }
        );

        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var guideCf = new CustomFormatResource
        {
            Name = "one",
            TrashId = "cf1",
            IncludeCustomFormatWhenRenaming = true,
        };

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(new TrashIdMapping("cf1", "one", 1)),
            ApiFetchOutput =
            [
                new CustomFormatResource { Name = "two", Id = 2 },
                new CustomFormatResource { Name = "one", Id = 1 },
            ],
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
                        NewCf.Data("one", "cf1", 1) with
                        {
                            IncludeCustomFormatWhenRenaming = true,
                        },
                    },
                }
            );
    }

    [Test]
    public async Task Unchanged_cfs_with_replace_enabled()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = true,
            }
        );

        var sut = scope.Resolve<CustomFormatTransactionPhase>();
        var guideCf = NewCf.Data("one", "cf1");

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(),
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
    public async Task Unchanged_cfs_without_replace()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = false,
            }
        );

        var sut = scope.Resolve<CustomFormatTransactionPhase>();
        var guideCf = NewCf.Data("one", "cf1");

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(new TrashIdMapping("cf1", "one", 1)),
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
    public async Task Deleted_cfs_when_enabled()
    {
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
            Cache = CfCache.New(new TrashIdMapping("cf2", "two", 2)),
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
    public async Task No_deleted_cfs_when_disabled()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                DeleteOldCustomFormats = false,
            }
        );

        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(new TrashIdMapping("cf2", "two", 2)),
            ApiFetchOutput = [new CustomFormatResource { Name = "two", Id = 2 }],
            Plan = new PipelinePlan(), // Empty plan
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.Should().BeEquivalentTo(new CustomFormatTransactionData());
    }

    [Test]
    public async Task Do_not_delete_cfs_in_config()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());

        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(new TrashIdMapping("cf2", "two", 2)),
            ApiFetchOutput = [new CustomFormatResource { Name = "two", Id = 2 }],
            Plan = CreatePlan(NewCf.Data("two", "cf2", 2)),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.DeletedCustomFormats.Should().BeEmpty();
    }

    [Test]
    public async Task Add_new_cf_when_in_cache_but_not_in_service()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(NewConfig.Radarr());

        var sut = scope.Resolve<CustomFormatTransactionPhase>();

        var context = new CustomFormatPipelineContext
        {
            Cache = CfCache.New(new TrashIdMapping("cf2", "two", 200)),
            ApiFetchOutput = [],
            Plan = CreatePlan(NewCf.Data("two", "cf2", 2)),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new CustomFormatTransactionData
                {
                    NewCustomFormats =
                    {
                        new CustomFormatResource
                        {
                            Name = "two",
                            TrashId = "cf2",
                            Id = 200,
                        },
                    },
                }
            );
    }

    [Test]
    public async Task Unchanged_when_specifications_match()
    {
        var scopeFactory = Resolve<ConfigurationScopeFactory>();
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = true,
            }
        );

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
            Cache = CfCache.New(),
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
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = true,
            }
        );

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
            Cache = CfCache.New(),
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
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = true,
            }
        );

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
            Cache = CfCache.New(),
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
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = true,
            }
        );

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
            Cache = CfCache.New(),
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
        using var scope = scopeFactory.Start<TestConfigurationScope>(
            NewConfig.Radarr() with
            {
                ReplaceExistingCustomFormats = true,
            }
        );

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
            Cache = CfCache.New(),
            ApiFetchOutput = [serviceCf],
            Plan = CreatePlan(guideCf),
        };

        await sut.Execute(context, CancellationToken.None);

        context.TransactionOutput.UnchangedCustomFormats.Should().ContainSingle();
        context.TransactionOutput.UpdatedCustomFormats.Should().BeEmpty();
    }
}
