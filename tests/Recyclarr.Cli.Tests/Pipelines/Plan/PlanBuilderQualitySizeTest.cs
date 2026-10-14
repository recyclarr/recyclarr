using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

internal sealed class PlanBuilderQualitySizeTest : PlanBuilderTestBase
{
    [Test]
    public void Build_with_quality_definition_produces_quality_sizes_in_plan()
    {
        SetupQualitySizeGuideData("movie", ("Bluray-1080p", 5, 100, 50), ("WEB-1080p", 3, 80, 40));

        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "movie" },
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.QualitySizes.Should().NotBeNull();
        plan.QualitySizes.Type.Should().Be("movie");
        plan.QualitySizes.Qualities.Should().HaveCount(2);
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    private static readonly Version SonarrV4 = new("4.0.20.3012");
    private static readonly Version SonarrV5 = new("5.0.0.2569");

    private void SetupSonarrSizeTypes()
    {
        SetupQualitySizeGuideData<SonarrQualitySizeResource>("series", ("HDTV-1080p", 5, 50, 30));
        SetupQualitySizeGuideData<SonarrQualitySizeResource>("anime", ("HDTV-1080p", 2, 20, 10));
        SetupQualitySizeGuideData<SonarrQualitySizeResource>("guide", ("HDTV-1080p", 7, 70, 35));
    }

    private static QualityDefinitionConfig Qd(string type) => new() { Type = type };

    [Test]
    public void Sonarr_v4_plans_global_sizes_without_warning()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with { QualityDefinition = Qd("series") };

        var (sut, publisher) = CreatePlanBuilder(config, SonarrV4);
        var plan = sut.Build();

        plan.QualitySizes.Type.Should().Be("series");
        publisher.DidNotReceiveWithAnyArgs().AddWarning(default!);
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Sonarr_v5_size_only_config_warns_and_plans_nothing()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with { QualityDefinition = Qd("series") };

        var (sut, publisher) = CreatePlanBuilder(config, SonarrV5);
        var plan = sut.Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        publisher.Received().AddWarning(Arg.Is<string>(s => s.Contains("quality_profiles")));
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
    }

    [Test]
    public void Sonarr_v5_assigns_sizes_per_profile_by_precedence()
    {
        SetupSonarrSizeTypes();
        SetupSonarrQualityProfileGuideData("guide-ref", "Guide Ref", ("guide", "qs-guide"));
        SetupSonarrQualityProfileGuideData("guide-noref", "Guide NoRef");

        var config = NewConfig.Sonarr() with
        {
            QualityDefinition = Qd("series"),
            QualityProfiles =
            [
                new QualityProfileConfig
                {
                    TrashId = "guide-ref",
                    Name = "Yaml Wins",
                    QualityDefinition = Qd("anime"),
                },
                new QualityProfileConfig { TrashId = "guide-ref" },
                new QualityProfileConfig { TrashId = "guide-noref" },
                new QualityProfileConfig { Name = "User Defined" },
            ],
        };

        var (sut, publisher) = CreatePlanBuilder(config, SonarrV5);
        var plan = sut.Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        publisher.DidNotReceiveWithAnyArgs().AddError(default!);
        publisher.DidNotReceiveWithAnyArgs().AddWarning(default!);
        plan.QualityProfiles.Should()
            .BeEquivalentTo(
                [
                    new { Name = "Yaml Wins", QualitySizes = new { Type = "anime" } },
                    new { Name = "Guide Ref", QualitySizes = new { Type = "guide" } },
                    new { Name = "Guide NoRef", QualitySizes = new { Type = "series" } },
                    new { Name = "User Defined", QualitySizes = new { Type = "series" } },
                ],
                o => o.ExcludingMissingMembers()
            );
    }

    [Test]
    public void Sonarr_v5_profile_without_any_size_source_gets_none()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = [new QualityProfileConfig { Name = "No Sizes" }],
        };

        var (sut, _) = CreatePlanBuilder(config, SonarrV5);
        var plan = sut.Build();

        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
    }

    [Test]
    public void Sonarr_v5_implicit_profiles_get_no_sizes()
    {
        SetupSonarrSizeTypes();
        var cf = new SonarrCustomFormatResource { TrashId = "cf1", Name = "CF One" };
        Fs.AddFile(
            "/guide/sonarr/cf/cf1.json",
            new MockFileData(JsonSerializer.Serialize(cf, GlobalJsonSerializerSettings.Guide))
        );
        Resolve<ResourceRegistry<IFileInfo>>()
            .Register<SonarrCustomFormatResource>([Fs.FileInfo.New("/guide/sonarr/cf/cf1.json")]);

        var (sut, _) = CreatePlanBuilder(
            NewConfig.Sonarr() with
            {
                QualityDefinition = Qd("series"),
                CustomFormats =
                [
                    new CustomFormatConfig
                    {
                        TrashIds = ["cf1"],
                        AssignScoresTo = [new AssignScoresToConfig { Name = "Implicit" }],
                    },
                ],
            },
            SonarrV5
        );
        var plan = sut.Build();

        plan.QualityProfiles.Should()
            .ContainSingle(x => x.Name == "Implicit")
            .Which.QualitySizes.Should()
            .BeNull();
    }

    [Test]
    public void Sonarr_v5_unknown_profile_size_type_is_an_error()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles =
            [
                new QualityProfileConfig { Name = "Bad", QualityDefinition = Qd("missing") },
            ],
        };

        var (sut, publisher) = CreatePlanBuilder(config, SonarrV5);
        sut.Build();

        publisher
            .Received()
            .AddError(Arg.Is<string>(s => s.Contains("'Bad'") && s.Contains("missing")));
    }

    [Test]
    public void Sonarr_v5_unknown_guide_size_reference_is_an_error()
    {
        SetupSonarrSizeTypes();
        SetupSonarrQualityProfileGuideData("guide-ref", "Guide Ref", ("guide", "missing-id"));
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "guide-ref" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config, SonarrV5);
        sut.Build();

        publisher
            .Received()
            .AddError(Arg.Is<string>(s => s.Contains("'Guide Ref'") && s.Contains("missing-id")));
    }

    [Test]
    public void Sonarr_v5_guide_reference_with_multiple_entries_is_an_error()
    {
        SetupSonarrSizeTypes();
        SetupSonarrQualityProfileGuideData(
            "guide-ref",
            "Guide Ref",
            ("series", "qs-series"),
            ("anime", "qs-anime")
        );
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "guide-ref" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config, SonarrV5);
        var plan = sut.Build();

        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
        publisher
            .Received()
            .AddError(
                Arg.Is<string>(s => s.Contains("'Guide Ref'") && s.Contains("more than one"))
            );
    }

    [Test]
    public void Sonarr_v4_ignores_guide_profile_size_reference_silently()
    {
        SetupSonarrSizeTypes();
        SetupSonarrQualityProfileGuideData("guide-ref", "Guide Ref", ("guide", "qs-guide"));
        var config = NewConfig.Sonarr() with
        {
            QualityProfiles = [new QualityProfileConfig { TrashId = "guide-ref" }],
        };

        var (sut, publisher) = CreatePlanBuilder(config, SonarrV4);
        var plan = sut.Build();

        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
        publisher.DidNotReceiveWithAnyArgs().AddWarning(default!);
    }

    [Test]
    public void Sonarr_v4_ignores_profile_size_source_with_warning_and_keeps_global_sizes()
    {
        SetupSonarrSizeTypes();
        var config = NewConfig.Sonarr() with
        {
            QualityDefinition = Qd("series"),
            QualityProfiles =
            [
                new QualityProfileConfig { Name = "Override", QualityDefinition = Qd("anime") },
            ],
        };

        var (sut, publisher) = CreatePlanBuilder(config, SonarrV4);
        var plan = sut.Build();

        plan.QualitySizes.Type.Should().Be("series");
        plan.QualityProfiles.Should().ContainSingle().Which.QualitySizes.Should().BeNull();
        publisher.Received().AddWarning(Arg.Is<string>(s => s.Contains("'Override'")));
    }

    [Test]
    public void Build_with_invalid_quality_type_reports_error()
    {
        var config = NewConfig.Radarr() with
        {
            QualityDefinition = new QualityDefinitionConfig { Type = "nonexistent" },
        };

        var (sut, publisher) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.QualitySizesAvailable.Should().BeFalse();
        publisher.Received().AddError(Arg.Is<string>(s => s.Contains("nonexistent")));
    }
}
