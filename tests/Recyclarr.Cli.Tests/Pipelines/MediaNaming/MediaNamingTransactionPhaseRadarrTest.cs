using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[TestFixture]
[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "Do not care about disposal in a testing context"
)]
internal sealed class MediaNamingTransactionPhaseRadarrTest
{
    [Test, AutoMockData]
    public async Task Radarr_left_null(MediaNamingTransactionPhase sut)
    {
        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new RadarrMediaNamingDto(),
            ConfigOutput = new ProcessedNamingConfig
            {
                Dto = new RadarrMediaNamingDto
                {
                    RenameMovies = true,
                    StandardMovieFormat = "file_format",
                    MovieFolderFormat = "folder_format",
                },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(context.ConfigOutput.Dto, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_right_null(MediaNamingTransactionPhase sut)
    {
        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new RadarrMediaNamingDto
            {
                RenameMovies = true,
                StandardMovieFormat = "file_format",
                MovieFolderFormat = "folder_format",
            },
            ConfigOutput = new ProcessedNamingConfig { Dto = new RadarrMediaNamingDto() },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(context.ApiFetchOutput, o => o.PreferringRuntimeMemberTypes());
    }

    [Test, AutoMockData]
    public async Task Radarr_right_and_left_with_rename(MediaNamingTransactionPhase sut)
    {
        var context = new MediaNamingPipelineContext
        {
            ApiFetchOutput = new RadarrMediaNamingDto
            {
                RenameMovies = false,
                StandardMovieFormat = "file_format",
                MovieFolderFormat = "folder_format",
            },
            ConfigOutput = new ProcessedNamingConfig
            {
                Dto = new RadarrMediaNamingDto
                {
                    RenameMovies = true,
                    StandardMovieFormat = "file_format2",
                    MovieFolderFormat = "folder_format2",
                },
            },
        };

        await sut.Execute(context, CancellationToken.None);

        context
            .TransactionOutput.Should()
            .BeEquivalentTo(context.ConfigOutput.Dto, o => o.PreferringRuntimeMemberTypes());
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
            ConfigOutput = new ProcessedNamingConfig
            {
                Dto = new RadarrMediaNamingDto
                {
                    RenameMovies = false,
                    StandardMovieFormat = "file_format2",
                    MovieFolderFormat = "folder_format2",
                },
            },
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
