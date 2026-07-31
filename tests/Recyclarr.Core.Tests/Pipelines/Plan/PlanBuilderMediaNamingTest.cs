using System.IO.Abstractions;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Core.Tests.Pipelines.Plan;

internal sealed class PlanBuilderMediaNamingTest : PlanBuilderTestBase
{
    private void SetupRadarrMediaNamingGuideData(
        IReadOnlyDictionary<string, string>? folderFormats = null,
        IReadOnlyDictionary<string, string>? fileFormats = null
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var resource = new RadarrMediaNamingResource
        {
            Folder = folderFormats ?? new Dictionary<string, string> { ["default"] = "{Movie}" },
            File = fileFormats ?? new Dictionary<string, string> { ["standard"] = "{Movie}.{ext}" },
        };
        var file = Fs.CurrentDirectory()
            .SubDirectory("guide", "radarr", "naming")
            .File("radarr-naming.json");
        Fs.AddJsonFile(file, resource, GlobalJsonSerializerSettings.Guide);
        registry.Register<RadarrMediaNamingResource>([file]);
    }

    private void SetupSonarrMediaNamingGuideData(
        IReadOnlyDictionary<string, string>? standardEpisodeFormats = null
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var resource = new SonarrMediaNamingResource
        {
            Season = new Dictionary<string, string> { ["default"] = "season" },
            Series = new Dictionary<string, string> { ["default"] = "series" },
            Episodes = new SonarrEpisodeNamingResource
            {
                Standard =
                    standardEpisodeFormats
                    ?? new Dictionary<string, string>
                    {
                        ["default"] = "standard",
                        ["default:4"] = "standard-v4",
                    },
            },
        };
        var file = Fs.CurrentDirectory()
            .SubDirectory("guide", "sonarr", "naming")
            .File("naming.json");
        Fs.AddJsonFile(file, resource, GlobalJsonSerializerSettings.Guide);
        registry.Register<SonarrMediaNamingResource>([file]);
    }

    [Test]
    public void Build_with_valid_media_naming_produces_plan()
    {
        SetupRadarrMediaNamingGuideData();

        var config = NewConfig.Radarr() with
        {
            MediaNaming = new RadarrMediaNamingConfig
            {
                Folder = "default",
                Movie = new RadarrMovieNamingConfig { Standard = "standard", Rename = true },
            },
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.RadarrMediaNaming.Should().NotBeNull();
        plan.Outcomes.Should().BeEmpty();
    }

    [Test]
    public void Build_with_invalid_media_naming_reports_diagnostics()
    {
        SetupRadarrMediaNamingGuideData();

        var config = NewConfig.Radarr() with
        {
            MediaNaming = new RadarrMediaNamingConfig
            {
                Folder = "default",
                Movie = new RadarrMovieNamingConfig { Standard = "nonexistent", Rename = true },
            },
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.Outcomes.Should()
            .ContainSingle()
            .Which.Should()
            .Be(new InvalidNamingFormatOutcome("Standard Movie Format", "nonexistent"));
    }

    [Test]
    public void Build_with_invalid_media_naming_reports_resource_local_error()
    {
        SetupRadarrMediaNamingGuideData();

        var config = NewConfig.Radarr() with
        {
            MediaNaming = new RadarrMediaNamingConfig
            {
                Folder = "default",
                Movie = new RadarrMovieNamingConfig { Standard = "nonexistent", Rename = true },
            },
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.HasErrors.Should().BeTrue();
        plan.RadarrMediaNamingAvailable.Should().BeTrue();
        plan.RadarrMediaNaming.Mismatches.Should()
            .ContainSingle()
            .Which.Should()
            .Be(
                new RadarrNamingReferenceMismatchOutcome(
                    RadarrNamingFormatField.StandardMovieFormat,
                    "nonexistent"
                )
            );
    }

    [Test]
    public void Build_with_only_invalid_media_naming_retains_failed_work()
    {
        SetupRadarrMediaNamingGuideData();
        var config = NewConfig.Radarr() with
        {
            MediaNaming = new RadarrMediaNamingConfig
            {
                Movie = new RadarrMovieNamingConfig { Standard = "Unknown" },
            },
        };

        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.RadarrMediaNamingAvailable.Should().BeTrue();
        plan.RadarrMediaNaming.Data.HasValues().Should().BeFalse();
        plan.RadarrMediaNaming.Mismatches.Should()
            .Equal(
                new RadarrNamingReferenceMismatchOutcome(
                    RadarrNamingFormatField.StandardMovieFormat,
                    "Unknown"
                )
            );
    }

    [Test]
    public void Build_with_empty_media_naming_omits_plan()
    {
        SetupRadarrMediaNamingGuideData();
        var (sut, _) = CreatePlanBuilder(NewConfig.Radarr());

        var plan = sut.Build();

        plan.RadarrMediaNamingAvailable.Should().BeFalse();
    }

    [Test]
    public void Build_with_false_rename_retains_plan()
    {
        SetupRadarrMediaNamingGuideData();
        var config = NewConfig.Radarr() with
        {
            MediaNaming = new RadarrMediaNamingConfig
            {
                Movie = new RadarrMovieNamingConfig { Rename = false },
            },
        };
        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.RadarrMediaNaming.Data.RenameMovies.Should().BeFalse();
    }

    [Test]
    public void Build_sonarr_with_mixed_keys_retains_valid_values_and_typed_mismatch()
    {
        SetupSonarrMediaNamingGuideData();
        var config = NewConfig.Sonarr() with
        {
            MediaNaming = new SonarrMediaNamingConfig
            {
                Series = "DEFAULT",
                Episodes = new SonarrEpisodeNamingConfig
                {
                    Standard = "default",
                    Daily = "Missing",
                },
            },
        };
        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.SonarrMediaNaming.Data.SeriesFolderFormat.Should().Be("series");
        plan.SonarrMediaNaming.Data.StandardEpisodeFormat.Should().Be("standard-v4");
        plan.SonarrMediaNaming.Mismatches.Should()
            .Equal(
                new SonarrNamingReferenceMismatchOutcome(
                    SonarrNamingFormatField.DailyEpisodeFormat,
                    "Missing"
                )
            );
    }

    [Test]
    public void Build_sonarr_falls_back_to_unsuffixed_episode_format()
    {
        SetupSonarrMediaNamingGuideData(
            new Dictionary<string, string> { ["standard"] = "standard-base" }
        );
        var config = NewConfig.Sonarr() with
        {
            MediaNaming = new SonarrMediaNamingConfig
            {
                Episodes = new SonarrEpisodeNamingConfig { Standard = "standard" },
            },
        };
        var (sut, _) = CreatePlanBuilder(config);

        var plan = sut.Build();

        plan.SonarrMediaNaming.Data.StandardEpisodeFormat.Should().Be("standard-base");
    }
}
