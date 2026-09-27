using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Net;
using Autofac;
using NSubstitute.ExceptionExtensions;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Refit;

namespace Recyclarr.Cli.Tests.Console.Commands;

/// <summary>
/// Runs list and delete command lines against the in-process server, so parsing, the generated
/// client, server responses, rendering, and exit codes are all exercised together.
/// </summary>
internal sealed class ServerBackedCommandsHttpTest : CliServerHttpFixture
{
    private readonly ICustomFormatService _customFormats = Substitute.For<ICustomFormatService>();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(_customFormats).As<ICustomFormatService>();
    }

    private void AddGuide()
    {
        var root = Fs.CurrentDirectory().SubDirectory("guide");
        Fs.AddFile(
            root.File("metadata.json"),
            new MockFileData(
                """
                {
                  "json_paths": {
                    "radarr": {
                      "custom_formats": ["cf"], "qualities": [], "naming": ["naming"],
                      "quality_profiles": [], "custom_format_groups": []
                    },
                    "sonarr": {
                      "custom_formats": [], "qualities": [], "naming": [],
                      "quality_profiles": [], "custom_format_groups": []
                    }
                  }
                }
                """
            )
        );
        Fs.AddFile(
            root.SubDirectory("cf").File("hdr.json"),
            new MockFileData("""{ "trash_id": "cf-hdr", "name": "HDR", "specifications": [] }""")
        );
        Fs.AddFile(
            root.SubDirectory("naming").File("radarr-naming.json"),
            new MockFileData("""{ "folder": { "plex:4": "{Movie Title}" }, "file": {} }""")
        );
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                $"""
                resource_providers:
                  - name: local-guide
                    type: trash-guides
                    path: {root.FullName}
                """
            )
        );
    }

    [Test]
    public async Task List_custom_formats_prints_guide_data_from_the_server()
    {
        AddGuide();

        var exitCode = await RunCliAsync("list", "custom-formats", "radarr", "--raw");

        exitCode.Should().Be(0);
        ConsoleOutput.Should().Contain("cf-hdr\tHDR");
    }

    [Test]
    public async Task List_naming_keeps_the_radarr_raw_format()
    {
        AddGuide();

        var exitCode = await RunCliAsync("list", "naming", "radarr", "--raw");

        exitCode.Should().Be(0);
        ConsoleOutput.Should().Contain("movie_folder\tplex (v4)\t{Movie Title}");
    }

    [Test]
    public async Task Delete_removes_named_custom_formats_through_the_server()
    {
        AddInstanceConfig("movies");
        _customFormats
            .GetCustomFormats(default)
            .ReturnsForAnyArgs([
                new CustomFormatResource { Id = 1, Name = "HDR" },
                new CustomFormatResource { Id = 2, Name = "Keep" },
            ]);

        var exitCode = await RunCliAsync("delete", "custom-formats", "movies", "hdr", "--force");

        exitCode.Should().Be(0);
        await _customFormats.Received().DeleteCustomFormat(1, Arg.Any<CancellationToken>());
        await _customFormats.DidNotReceive().DeleteCustomFormat(2, Arg.Any<CancellationToken>());
        LogOutput.Should().Contain("Deleted 1 custom formats");
    }

    [Test]
    public async Task Delete_for_an_unknown_instance_fails_with_the_server_reason()
    {
        AddInstanceConfig("movies");

        var exitCode = await RunCliAsync("delete", "custom-formats", "nope", "--all", "--force");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("Instance is not configured");
    }

    [Test]
    public async Task Service_failure_is_reported_by_category()
    {
        AddInstanceConfig("movies");
        _customFormats
            .GetCustomFormats(default)
            .ThrowsAsyncForAnyArgs(await CreateApiException(HttpStatusCode.Unauthorized));

        var exitCode = await RunCliAsync("delete", "custom-formats", "movies", "--all", "--force");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("rejected the API key");
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "ApiException.Create takes ownership of request and response"
    )]
    private static Task<ApiException> CreateApiException(HttpStatusCode statusCode)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:7878/api/v3/cf");
        var response = new HttpResponseMessage(statusCode);
        return ApiException.Create(request, HttpMethod.Get, response, new RefitSettings());
    }
}
