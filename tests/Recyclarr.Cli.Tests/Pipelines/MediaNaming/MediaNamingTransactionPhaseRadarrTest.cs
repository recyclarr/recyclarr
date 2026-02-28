using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.MediaNaming.Radarr;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "Do not care about disposal in a testing context"
)]
internal sealed class MediaNamingTransactionPhaseRadarrTest
{
    private static TestPlan CreatePlan(RadarrNamingData data)
    {
        var plan = new TestPlan
        {
            RadarrMediaNaming = new PlannedRadarrMediaNaming { Data = data },
        };
        return plan;
    }

    [Test, AutoMockData]
    public async Task Radarr_left_null(RadarrNamingTransactionPhase sut)
    {
        var configData = new RadarrNamingData
        {
            RenameMovies = true,
            StandardMovieFormat = "file_format",
            MovieFolderFormat = "folder_format",
        };

        var context = new RadarrNamingPipelineContext
        {
            ApiFetchOutput = new RadarrNamingData(),
            Plan = CreatePlan(configData),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(configData, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_right_null(RadarrNamingTransactionPhase sut)
    {
        var apiData = new RadarrNamingData
        {
            RenameMovies = true,
            StandardMovieFormat = "file_format",
            MovieFolderFormat = "folder_format",
        };

        var context = new RadarrNamingPipelineContext
        {
            ApiFetchOutput = apiData,
            Plan = CreatePlan(new RadarrNamingData()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(apiData, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_right_and_left_with_rename(RadarrNamingTransactionPhase sut)
    {
        var configData = new RadarrNamingData
        {
            RenameMovies = true,
            StandardMovieFormat = "file_format2",
            MovieFolderFormat = "folder_format2",
        };

        var context = new RadarrNamingPipelineContext
        {
            ApiFetchOutput = new RadarrNamingData
            {
                RenameMovies = false,
                StandardMovieFormat = "file_format",
                MovieFolderFormat = "folder_format",
            },
            Plan = CreatePlan(configData),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(configData, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_right_and_left_without_rename(RadarrNamingTransactionPhase sut)
    {
        var context = new RadarrNamingPipelineContext
        {
            ApiFetchOutput = new RadarrNamingData
            {
                RenameMovies = true,
                StandardMovieFormat = "file_format",
                MovieFolderFormat = "folder_format",
            },
            Plan = CreatePlan(
                new RadarrNamingData
                {
                    RenameMovies = false,
                    StandardMovieFormat = "file_format2",
                    MovieFolderFormat = "folder_format2",
                }
            ),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(
                new RadarrNamingData
                {
                    RenameMovies = false,
                    StandardMovieFormat = "file_format2",
                    MovieFolderFormat = "folder_format2",
                },
                o => o.PreferringRuntimeMemberTypes()
            );
    }
}
