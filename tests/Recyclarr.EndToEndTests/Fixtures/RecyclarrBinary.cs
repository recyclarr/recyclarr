using System.IO.Abstractions;
using System.Reflection;
using CliWrap;
using TUnit.Core.Interfaces;

namespace Recyclarr.EndToEndTests.Fixtures;

// Publishes the Recyclarr CLI binary once per test session.
internal sealed class RecyclarrBinary : IAsyncInitializer, IAsyncDisposable
{
    private static readonly FileSystem FileSystem = new();
    private IDirectoryInfo _publishDir = null!;

    public IFileInfo Binary { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var guid = Guid.NewGuid();
        var tempDir = FileSystem.DirectoryInfo.New(FileSystem.Path.GetTempPath());
        _publishDir = tempDir.SubDirectory($"recyclarr-e2e-publish-{guid}");

        Binary = _publishDir.File("recyclarr");

        var repoRoot = FileSystem.DirectoryInfo.New(GetRepositoryRoot());
        var cliProjectDir = repoRoot.SubDirectory("src").SubDirectory("Recyclarr.Cli");

        await Cli.Wrap("dotnet")
            .WithArguments([
                "publish",
                cliProjectDir.FullName,
                "-c",
                "Release",
                "--self-contained",
                "-o",
                _publishDir.FullName,
            ])
            .ExecuteAsync();
    }

    public ValueTask DisposeAsync()
    {
        if (_publishDir.Exists)
        {
            _publishDir.Delete(true);
        }

        return ValueTask.CompletedTask;
    }

    private static string GetRepositoryRoot()
    {
        var assembly = typeof(RecyclarrBinary).Assembly;
        var attribute = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RecyclarrRepositoryRoot");

        if (attribute?.Value is null)
        {
            throw new InvalidOperationException(
                "RecyclarrRepositoryRoot assembly metadata not found."
            );
        }

        return Path.GetFullPath(attribute.Value);
    }
}
