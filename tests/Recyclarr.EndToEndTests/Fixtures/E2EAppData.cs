using System.IO.Abstractions;
using TUnit.Core.Interfaces;

namespace Recyclarr.EndToEndTests.Fixtures;

// Creates a temporary app data directory with test fixtures (settings, custom formats, overrides).
// Shared per test session so all tests use the same prepared directory.
internal sealed class E2EAppData : IAsyncInitializer, IAsyncDisposable
{
    private static readonly FileSystem FileSystem = new();

    public IDirectoryInfo TestDataDir { get; private set; } = null!;
    public IDirectoryInfo AppDataDir { get; private set; } = null!;
    public IFileInfo ConfigFile { get; private set; } = null!;
    public IFileInfo ConfigFileDeleteDisabled { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var guid = Guid.NewGuid();
        var tempDir = FileSystem.DirectoryInfo.New(FileSystem.Path.GetTempPath());
        AppDataDir = tempDir.SubDirectory($"recyclarr-e2e-appdata-{guid}");
        AppDataDir.Create();

        TestDataDir = FileSystem
            .DirectoryInfo.New(AppDomain.CurrentDomain.BaseDirectory)
            .SubDirectory("TestData");

        ConfigFile = TestDataDir.File("recyclarr.yml");
        ConfigFileDeleteDisabled = TestDataDir.File("recyclarr-delete-disabled.yml");

        await SetUpFixtures();
    }

    public ValueTask DisposeAsync()
    {
        if (AppDataDir.Exists)
        {
            AppDataDir.Delete(true);
        }

        return ValueTask.CompletedTask;
    }

    private async Task SetUpFixtures()
    {
        var sonarrCfsSource = TestDataDir.SubDirectory("custom-formats-sonarr");
        var sonarrCfsDest = AppDataDir.SubDirectory("custom-formats-sonarr");
        if (sonarrCfsSource.Exists)
        {
            CopyDirectory(sonarrCfsSource, sonarrCfsDest);
        }

        var radarrCfsSource = TestDataDir.SubDirectory("custom-formats-radarr");
        var radarrCfsDest = AppDataDir.SubDirectory("custom-formats-radarr");
        if (radarrCfsSource.Exists)
        {
            CopyDirectory(radarrCfsSource, radarrCfsDest);
        }

        var overrideSource = TestDataDir.SubDirectory("trash-guides-override");
        var overrideDest = AppDataDir.SubDirectory("trash-guides-override");
        if (overrideSource.Exists)
        {
            CopyDirectory(overrideSource, overrideDest);
        }

        // Replace path placeholders in settings with actual temp directory paths
        var settingsSource = TestDataDir.File("settings.yml");
        if (settingsSource.Exists)
        {
            var settingsContent = await FileSystem.File.ReadAllTextAsync(settingsSource.FullName);
            settingsContent = settingsContent
                .Replace(
                    "PLACEHOLDER_SONARR_CFS_PATH",
                    sonarrCfsDest.FullName,
                    StringComparison.Ordinal
                )
                .Replace(
                    "PLACEHOLDER_RADARR_CFS_PATH",
                    radarrCfsDest.FullName,
                    StringComparison.Ordinal
                )
                .Replace(
                    "PLACEHOLDER_OVERRIDE_PATH",
                    overrideDest.FullName,
                    StringComparison.Ordinal
                );
            var settingsDest = AppDataDir.File("settings.yml");
            await FileSystem.File.WriteAllTextAsync(settingsDest.FullName, settingsContent);
        }
    }

    private static void CopyDirectory(IDirectoryInfo sourceDir, IDirectoryInfo destDir)
    {
        destDir.Create();

        foreach (var file in sourceDir.GetFiles())
        {
            file.CopyTo(destDir.File(file.Name).FullName);
        }

        foreach (var dir in sourceDir.GetDirectories())
        {
            CopyDirectory(dir, destDir.SubDirectory(dir.Name));
        }
    }
}
