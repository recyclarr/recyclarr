using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Tests.Pipelines.MediaNaming;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class MediaNamingTransactionPhaseRadarrTest
{
    [Test, AutoMockData]
    public void Radarr_left_null(
        MediaNamingTransactionPhase sut)
    {
        var left = new RadarrMediaNamingDto();

        var right = new ProcessedNamingConfig
        {
            Dto = new RadarrMediaNamingDto
            {
                RenameMovies = true,
                StandardMovieFormat = "file_format",
                MovieFolderFormat = "folder_format"
            }
        };

        var result = sut.Execute(left, right);

        result.Should().BeEquivalentTo(right.Dto, o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public void Radarr_right_null(
        MediaNamingTransactionPhase sut)
    {
        var left = new RadarrMediaNamingDto
        {
            RenameMovies = true,
            StandardMovieFormat = "file_format",
            MovieFolderFormat = "folder_format"
        };

        var right = new ProcessedNamingConfig
        {
            Dto = new RadarrMediaNamingDto()
        };

        var result = sut.Execute(left, right);

        result.Should().BeEquivalentTo(left, o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public void Radarr_right_and_left_with_rename(
        MediaNamingTransactionPhase sut)
    {
        var left = new RadarrMediaNamingDto
        {
            RenameMovies = false,
            StandardMovieFormat = "file_format",
            MovieFolderFormat = "folder_format"
        };

        var right = new ProcessedNamingConfig
        {
            Dto = new RadarrMediaNamingDto
            {
                RenameMovies = true,
                StandardMovieFormat = "file_format2",
                MovieFolderFormat = "folder_format2"
            }
        };

        var result = sut.Execute(left, right);

        result.Should().BeEquivalentTo(right.Dto, o => o.RespectingRuntimeTypes());
    }

    [Test, AutoMockData]
    public void Radarr_right_and_left_without_rename(
        MediaNamingTransactionPhase sut)
    {
        var left = new RadarrMediaNamingDto
        {
            RenameMovies = true,
            StandardMovieFormat = "file_format",
            MovieFolderFormat = "folder_format"
        };

        var right = new ProcessedNamingConfig
        {
            Dto = new RadarrMediaNamingDto
            {
                RenameMovies = false,
                StandardMovieFormat = "file_format2",
                MovieFolderFormat = "folder_format2"
            }
        };

        var result = sut.Execute(left, right);

        result.Should().BeEquivalentTo(new RadarrMediaNamingDto
            {
                RenameMovies = false,
                StandardMovieFormat = "file_format",
                MovieFolderFormat = "folder_format2"
            },
            o => o.RespectingRuntimeTypes());
    }
}
