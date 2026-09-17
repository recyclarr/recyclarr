using System.IO.Abstractions;
using Autofac;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;
using Recyclarr.Compatibility.Sonarr;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class PlanningOutcomeIntegrationTest : CoreIntegrationTestFixture
{
    private GuideResourceTestData GuideData => new(Fs, Resolve<ResourceRegistry<IFileInfo>>());

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        var serviceInformation = Substitute.For<IServiceInformation>();
        serviceInformation.GetAppName(default).ReturnsForAnyArgs("Radarr");
        serviceInformation.GetVersion(default).ReturnsForAnyArgs(new Version(6, 0));
        builder.RegisterInstance(serviceInformation).As<IServiceInformation>();

        var capabilities = Substitute.For<IRadarrCapabilityFetcher>();
        capabilities
            .GetCapabilities(default)
            .ReturnsForAnyArgs(new RadarrCapabilities(new Version(6, 0)));
        builder.RegisterInstance(capabilities).As<IRadarrCapabilityFetcher>();
        builder.RegisterInstance(Substitute.For<ISonarrCapabilityFetcher>());

        ISyncOperation[] operations = [new TestSyncOperation()];
        builder.RegisterInstance(operations.AsEnumerable()).As<IEnumerable<ISyncOperation>>();
    }

    [Test]
    public async Task Planning_validation_reasons_survive_completed_results_and_handled_failures()
    {
        AddGuideData();
        using var scope = Resolve<SyncRunScopeFactory>().Start<ISyncOrchestrator>();

        var result = await scope.Entry.RunAsync(
            [BuildInvalidConfig(), BuildNonBlockingConfig(), BuildHandledFailureConfig()],
            Substitute.For<ISyncSettings>(),
            Substitute.For<IInstanceSyncProgress>(),
            CancellationToken.None
        );

        result.Status.Should().Be(SyncResultStatus.Partial);
        var invalidInstance = result.Instances[0];
        invalidInstance.Status.Should().Be(SyncResultStatus.Failed);
        invalidInstance
            .Pipelines.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(SyncResultStatus.Failed);

        PlanningOutcome[] expected =
        [
            new CustomFormatGroupReferenceMismatchPlanningOutcome("missing-group"),
            new CustomFormatGroupSelectReferenceMismatchPlanningOutcome("group-id", "missing-cf"),
            new CustomFormatGroupRequiredItemSelectedPlanningOutcome("group-id", "required-cf"),
            new CustomFormatGroupDefaultItemSelectedPlanningOutcome("group-id", "default-cf"),
            new CustomFormatGroupExcludeReferenceMismatchPlanningOutcome("group-id", "missing-cf"),
            new CustomFormatGroupRequiredItemExcludedPlanningOutcome("group-id", "required-cf"),
            new CustomFormatGroupNonDefaultItemExcludedPlanningOutcome("group-id", "optional-cf"),
            new CustomFormatGroupQualityProfileReferenceMismatchPlanningOutcome(
                "group-id",
                "missing-profile"
            ),
            new CustomFormatQualityProfileReferenceAmbiguousPlanningOutcome(
                "profile-id",
                ["Profile A", "Profile B"]
            ),
            new CustomFormatGroupQualityProfileReferenceAmbiguousPlanningOutcome(
                "group-id",
                "profile-id",
                ["Profile A", "Profile B"]
            ),
        ];
        invalidInstance
            .PlanningOutcomes.Select(x => x.GetType())
            .Should()
            .Equal(expected.Select(x => x.GetType()));
        invalidInstance
            .PlanningOutcomes.Should()
            .BeEquivalentTo(
                expected,
                options => options.PreferringRuntimeMemberTypes().WithStrictOrdering()
            );

        var nonBlockingInstance = result.Instances[1];
        nonBlockingInstance.Status.Should().Be(SyncResultStatus.Succeeded);
        nonBlockingInstance
            .Pipelines.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(SyncResultStatus.Succeeded);
        nonBlockingInstance
            .PlanningOutcomes.Should()
            .Equal(
                new CustomFormatGroupRequiredItemSelectedPlanningOutcome("group-id", "required-cf"),
                new CustomFormatGroupDefaultItemSelectedPlanningOutcome("group-id", "default-cf"),
                new CustomFormatGroupRequiredItemExcludedPlanningOutcome("group-id", "required-cf"),
                new CustomFormatGroupNonDefaultItemExcludedPlanningOutcome(
                    "group-id",
                    "optional-cf"
                )
            );

        var handledFailureInstance = result.Instances[2];
        handledFailureInstance.Status.Should().Be(SyncResultStatus.Failed);
        handledFailureInstance.Failure.Should().BeOfType<ServiceUnavailableFailure>();
        handledFailureInstance
            .Pipelines.Should()
            .ContainSingle()
            .Which.Status.Should()
            .Be(SyncResultStatus.Failed);
        handledFailureInstance
            .PlanningOutcomes.Should()
            .Equal(
                new CustomFormatGroupDefaultItemSelectedPlanningOutcome("group-id", "default-cf")
            );
    }

    private void AddGuideData()
    {
        GuideData.AddCustomFormats(
            ("Direct CF", "direct-cf"),
            ("Failing CF", "failing-cf"),
            ("Required CF", "required-cf"),
            ("Default CF", "default-cf"),
            ("Optional CF", "optional-cf")
        );
        GuideData.AddQualityProfile("profile-id", "Guide Profile", ("HDTV-1080p", true, null));
        GuideData.AddCfGroup(
            "group-id",
            "Test Group",
            [
                new CfGroupCustomFormat
                {
                    TrashId = "required-cf",
                    Name = "Required CF",
                    Required = true,
                },
                new CfGroupCustomFormat
                {
                    TrashId = "default-cf",
                    Name = "Default CF",
                    Default = true,
                },
                new CfGroupCustomFormat { TrashId = "optional-cf", Name = "Optional CF" },
            ]
        );
    }

    private static RadarrConfiguration BuildInvalidConfig() =>
        new()
        {
            InstanceName = "radarr",
            BaseUrl = new Uri("http://localhost"),
            ApiKey = "api-key",
            CustomFormats =
            [
                new CustomFormatConfig
                {
                    TrashIds = ["direct-cf"],
                    AssignScoresTo = [new AssignScoresToConfig { TrashId = "profile-id" }],
                },
            ],
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig { TrashId = "missing-group" },
                    new CustomFormatGroupConfig
                    {
                        TrashId = "group-id",
                        Select = ["missing-cf", "required-cf", "default-cf"],
                        Exclude = ["missing-cf", "required-cf", "optional-cf"],
                        AssignScoresTo =
                        [
                            new AssignScoresToConfig { TrashId = "missing-profile" },
                            new AssignScoresToConfig { TrashId = "profile-id" },
                        ],
                    },
                ],
            },
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "profile-id", Name = "Profile A" },
                new QualityProfileConfig { TrashId = "profile-id", Name = "Profile B" },
            ],
        };

    private static RadarrConfiguration BuildNonBlockingConfig() =>
        new()
        {
            InstanceName = "radarr-non-blocking",
            BaseUrl = new Uri("http://localhost"),
            ApiKey = "api-key",
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "group-id",
                        Select = ["required-cf", "default-cf"],
                        Exclude = ["required-cf", "optional-cf"],
                        AssignScoresTo = [new AssignScoresToConfig { Name = "Profile A" }],
                    },
                ],
            },
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "profile-id", Name = "Profile A" },
            ],
        };

    private static RadarrConfiguration BuildHandledFailureConfig() =>
        new()
        {
            InstanceName = "radarr-handled-failure",
            BaseUrl = new Uri("http://localhost"),
            ApiKey = "api-key",
            CustomFormats =
            [
                new CustomFormatConfig
                {
                    TrashIds = ["failing-cf"],
                    AssignScoresTo = [new AssignScoresToConfig { Name = "Profile A" }],
                },
            ],
            CustomFormatGroups = new CustomFormatGroupsConfig
            {
                Add =
                [
                    new CustomFormatGroupConfig
                    {
                        TrashId = "group-id",
                        Select = ["default-cf"],
                        AssignScoresTo = [new AssignScoresToConfig { Name = "Profile A" }],
                    },
                ],
            },
            QualityProfiles =
            [
                new QualityProfileConfig { TrashId = "profile-id", Name = "Profile A" },
            ],
        };

    private sealed class TestSyncOperation : ISyncOperation
    {
        public PipelineType Type => PipelineType.CustomFormat;
        public string Description => "Test";
        public IReadOnlyList<PipelineType> Dependencies => [];

        public bool ShouldSkip(PipelinePlan plan) => false;

        public Task<PipelineResult> Execute(
            bool preview,
            PipelinePlan plan,
            IPipelinePublisher publisher,
            Action<PipelineResult> capture,
            CancellationToken ct
        )
        {
            if (plan.GetCustomFormat("failing-cf") is not null)
            {
                throw new HttpRequestException("Service unavailable");
            }

            var result = new TestPipelineResult(SyncResultStatus.Succeeded);
            capture(result);
            return Task.FromResult<PipelineResult>(result);
        }

        public PipelineResult CreateBlockedResult(PipelineType dependency) =>
            new TestPipelineResult(SyncResultStatus.Blocked, dependency);

        public PipelineResult CreateFailedResult(PipelineResult? current) =>
            new TestPipelineResult(SyncResultStatus.Failed);
    }

    private sealed record TestPipelineResult : PipelineResult
    {
        public TestPipelineResult(SyncResultStatus status, PipelineType? blockedBy = null)
            : base(status, blockedBy) { }

        internal override PipelineResult WithStatus(
            SyncResultStatus status,
            PipelineType? blockedBy = null
        ) => new TestPipelineResult(status, blockedBy);
    }
}
