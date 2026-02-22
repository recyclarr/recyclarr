using System.IO.Abstractions;
using Recyclarr.Platform;
using IEnvironment = Recyclarr.Platform.IEnvironment;

namespace Recyclarr.Core.Tests.Platform;

internal sealed class DefaultAppDataSetupTest
{
    [Test, AutoMockData]
    public void Initialize_using_default_path(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs((string?)null);

        var basePath = fs.CurrentDirectory().SubDirectory("base").SubDirectory("path");

        env.GetFolderPath(default, default).ReturnsForAnyArgs(basePath.FullName);

        var paths = sut.CreateAppPaths();

        paths.ConfigDirectory.FullName.Should().Be(basePath.SubDirectory("recyclarr").FullName);
    }

    [Test, AutoMockData]
    public void Creation_uses_correct_behavior(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        var appDataPath = fs.CurrentDirectory().SubDirectory("override").SubDirectory("path");

        env.GetEnvironmentVariable(default!).ReturnsForAnyArgs((string?)null);
        env.GetFolderPath(default).ReturnsForAnyArgs(appDataPath.FullName);

        sut.CreateAppPaths();

        fs.AllDirectories.Should().NotContain(appDataPath.FullName);
    }

    [Test, AutoMockData]
    public void Use_config_dir_environment_variable(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        var expectedPath = fs.CurrentDirectory()
            .SubDirectory("config")
            .SubDirectory("path")
            .FullName;

        env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR").Returns(expectedPath);
        env.GetEnvironmentVariable("RECYCLARR_APP_DATA").Returns((string?)null);
        env.GetEnvironmentVariable("RECYCLARR_DATA_DIR").Returns((string?)null);

        var paths = sut.CreateAppPaths();

        paths.ConfigDirectory.FullName.Should().Be(expectedPath);
        paths.YamlConfigDirectory.FullName.Should().StartWith(expectedPath);
    }

    [Test, AutoMockData]
    public void Use_data_dir_environment_variable(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        var configPath = fs.CurrentDirectory().SubDirectory("config").FullName;
        var dataPath = fs.CurrentDirectory().SubDirectory("data").FullName;

        env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR").Returns(configPath);
        env.GetEnvironmentVariable("RECYCLARR_DATA_DIR").Returns(dataPath);
        env.GetEnvironmentVariable("RECYCLARR_APP_DATA").Returns((string?)null);

        var paths = sut.CreateAppPaths();

        paths.LogDirectory.FullName.Should().StartWith(dataPath);
        paths.ResourceDirectory.FullName.Should().StartWith(dataPath);
    }

    [Test, AutoMockData]
    public void Data_dir_defaults_to_config_dir_when_unset(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        var configPath = fs.CurrentDirectory().SubDirectory("config").FullName;

        env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR").Returns(configPath);
        env.GetEnvironmentVariable("RECYCLARR_DATA_DIR").Returns((string?)null);
        env.GetEnvironmentVariable("RECYCLARR_APP_DATA").Returns((string?)null);

        var paths = sut.CreateAppPaths();

        // Both config and data directories should be under the same root
        paths.YamlConfigDirectory.FullName.Should().StartWith(configPath);
        paths.LogDirectory.FullName.Should().StartWith(configPath);
        paths.ResourceDirectory.FullName.Should().StartWith(configPath);
    }

    [Test, AutoMockData]
    public void Deprecated_app_data_variable_throws_error(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        env.GetEnvironmentVariable("RECYCLARR_APP_DATA").Returns("/some/path");

        var act = () => sut.CreateAppPaths();

        act.Should()
            .Throw<EnvironmentException>()
            .WithMessage("*RECYCLARR_APP_DATA is no longer supported*");
    }

    [Test, AutoMockData]
    public void Relative_data_dir_resolves_from_config_dir(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        var configPath = fs.CurrentDirectory().SubDirectory("config").FullName;

        env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR").Returns(configPath);
        env.GetEnvironmentVariable("RECYCLARR_DATA_DIR").Returns("../data");
        env.GetEnvironmentVariable("RECYCLARR_APP_DATA").Returns((string?)null);

        var paths = sut.CreateAppPaths();

        // Path is normalized by file system, so /config/../data becomes /data
        var expectedDataPath = fs.CurrentDirectory().SubDirectory("data").FullName;
        paths.LogDirectory.FullName.Should().StartWith(expectedDataPath);
    }

    [Test, AutoMockData]
    public void Use_only_data_dir_with_platform_default_for_config(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        var platformPath = fs.CurrentDirectory().SubDirectory("platform").SubDirectory("default");
        var dataPath = fs.CurrentDirectory().SubDirectory("data").FullName;

        env.GetFolderPath(default, default).ReturnsForAnyArgs(platformPath.FullName);
        env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR").Returns((string?)null);
        env.GetEnvironmentVariable("RECYCLARR_DATA_DIR").Returns(dataPath);
        env.GetEnvironmentVariable("RECYCLARR_APP_DATA").Returns((string?)null);

        var paths = sut.CreateAppPaths();

        // Config uses platform default
        var expectedConfigPath = platformPath.SubDirectory("recyclarr").FullName;
        paths.ConfigDirectory.FullName.Should().Be(expectedConfigPath);
        paths.YamlConfigDirectory.FullName.Should().StartWith(expectedConfigPath);

        // Data uses specified directory
        paths.LogDirectory.FullName.Should().StartWith(dataPath);
        paths.ResourceDirectory.FullName.Should().StartWith(dataPath);
    }

    [Test, AutoMockData]
    public void Platform_default_unavailable_throws_exception(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IEnvironment env,
        DefaultAppDataSetup sut
    )
    {
        env.GetFolderPath(default, default).ReturnsForAnyArgs(string.Empty);
        env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR").Returns((string?)null);
        env.GetEnvironmentVariable("RECYCLARR_DATA_DIR").Returns((string?)null);
        env.GetEnvironmentVariable("RECYCLARR_APP_DATA").Returns((string?)null);

        var act = () => sut.CreateAppPaths();

        act.Should()
            .Throw<EnvironmentException>()
            .WithMessage("*Unable to find or create the default app data directory*");
    }
}
