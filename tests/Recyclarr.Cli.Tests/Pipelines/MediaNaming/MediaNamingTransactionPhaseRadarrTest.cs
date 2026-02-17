using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Cli.Pipelines.Plan;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "Do not care about disposal in a testing context"
)]
internal sealed class MediaNamingTransactionPhaseRadarrTest
{
    private static TestPlan CreatePlan(MediaNamingDto dto)
    {
        var plan = new TestPlan { MediaNaming = new PlannedMediaNaming { Dto = dto } };
        return plan;
    }

    [Test, AutoMockData]
    public async Task Radarr_left_null(MediaNamingTransactionPhase sut)
    {
        var configDto = new RadarrMediaNamingDto
        {
            RenameMovies = true,
            StandardMovieFormat = "file_format",
            MovieFolderFormat = "folder_format",
        };

        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new RadarrMediaNamingDto(),
            Plan = CreatePlan(configDto),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(configDto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_right_null(MediaNamingTransactionPhase sut)
    {
        var apiDto = new RadarrMediaNamingDto
        {
            RenameMovies = true,
            StandardMovieFormat = "file_format",
            MovieFolderFormat = "folder_format",
        };

        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = apiDto,
            Plan = CreatePlan(new RadarrMediaNamingDto()),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(apiDto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_right_and_left_with_rename(MediaNamingTransactionPhase sut)
    {
        var configDto = new RadarrMediaNamingDto
        {
            RenameMovies = true,
            StandardMovieFormat = "file_format2",
            MovieFolderFormat = "folder_format2",
        };

        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new RadarrMediaNamingDto
            {
                RenameMovies = false,
                StandardMovieFormat = "file_format",
                MovieFolderFormat = "folder_format",
            },
            Plan = CreatePlan(configDto),
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(configDto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_right_and_left_without_rename(MediaNamingTransactionPhase sut)
    {
        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new RadarrMediaNamingDto
            {
                RenameMovies = true,
                StandardMovieFormat = "file_format",
                MovieFolderFormat = "folder_format",
            },
            Plan = CreatePlan(
                new RadarrMediaNamingDto
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
                new RadarrMediaNamingDto
                {
                    RenameMovies = false,
                    StandardMovieFormat = "file_format2",
                    MovieFolderFormat = "folder_format2",
                },
                o => o.PreferringRuntimeMemberTypes()
            );
    }
}
