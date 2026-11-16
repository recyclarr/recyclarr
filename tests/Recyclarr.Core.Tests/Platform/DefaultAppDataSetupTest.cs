using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.TestLibrary;

namespace Recyclarr.Core.Tests.Platform;

internal sealed class DefaultAppDataSetupTest
{
    private static DefaultAppDataSetup CreateSut(
        IFileSystem fs,
        TestEnvironment env,
        ISpecialFolders folders
    )
    {
        return new DefaultAppDataSetup(
            env.Configuration,
            fs,
            new ConfigDirectoryLocator(env.Configuration, folders, fs)
        );
    }

    [Test, AutoMockData]
    public void Initialize_using_default_path(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);

        var basePath = fs.CurrentDirectory().SubDirectory("base").SubDirectory("path");

        folders.GetFolderPath(default, default).ReturnsForAnyArgs(basePath.FullName);

        var paths = sut.CreateAppPaths();

        paths.ConfigDirectory.FullName.Should().Be(basePath.SubDirectory("recyclarr").FullName);
    }

    [Test, AutoMockData]
    public void Creation_uses_correct_behavior(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var appDataPath = fs.CurrentDirectory().SubDirectory("override").SubDirectory("path");

        sut.CreateAppPaths();

        fs.AllDirectories.Should().NotContain(appDataPath.FullName);
    }

    [Test, AutoMockData]
    public void Use_config_dir_environment_variable(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var expectedPath = fs.CurrentDirectory()
            .SubDirectory("config")
            .SubDirectory("path")
            .FullName;

        env.Set("RECYCLARR_CONFIG_DIR", expectedPath);

        var paths = sut.CreateAppPaths();

        paths.ConfigDirectory.FullName.Should().Be(expectedPath);
        paths.YamlConfigDirectory.FullName.Should().StartWith(expectedPath);
    }

    [Test, AutoMockData]
    public void Use_data_dir_environment_variable(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var configPath = fs.CurrentDirectory().SubDirectory("config").FullName;
        var dataPath = fs.CurrentDirectory().SubDirectory("data").FullName;

        env.Set("RECYCLARR_CONFIG_DIR", configPath);
        env.Set("RECYCLARR_DATA_DIR", dataPath);

        var paths = sut.CreateAppPaths();

        paths
            .ServerLogDirectory.FullName.Should()
            .Be(fs.CurrentDirectory().SubDirectory("data", "logs", "server").FullName);
        paths.ResourceDirectory.FullName.Should().StartWith(dataPath);
    }

    [Test, AutoMockData]
    public void Data_dir_defaults_to_config_dir_when_unset(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var configPath = fs.CurrentDirectory().SubDirectory("config").FullName;

        env.Set("RECYCLARR_CONFIG_DIR", configPath);

        var paths = sut.CreateAppPaths();

        // Both config and data directories should be under the same root
        paths.YamlConfigDirectory.FullName.Should().StartWith(configPath);
        paths.ServerLogDirectory.FullName.Should().StartWith(configPath);
        paths.ResourceDirectory.FullName.Should().StartWith(configPath);
    }

    [Test, AutoMockData]
    public void State_dir_defaults_under_config_dir_when_unset(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var configDir = fs.CurrentDirectory().SubDirectory("config");

        env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);

        var paths = sut.CreateAppPaths();

        paths.StateDirectory.FullName.Should().Be(configDir.SubDirectory("state").FullName);
        fs.AllDirectories.Should().Contain(paths.StateDirectory.FullName);
    }

    [Test, AutoMockData]
    public void Use_state_dir_environment_variable(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var configDir = fs.CurrentDirectory().SubDirectory("config");
        var dataDir = fs.CurrentDirectory().SubDirectory("data");
        var stateDir = fs.CurrentDirectory().SubDirectory("state");

        env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);
        env.Set("RECYCLARR_DATA_DIR", dataDir.FullName);
        env.Set("RECYCLARR_STATE_DIR", stateDir.FullName);

        var paths = sut.CreateAppPaths();

        paths.StateDirectory.FullName.Should().Be(stateDir.FullName);
        fs.AllDirectories.Should().Contain(stateDir.FullName);
    }

    [Test, AutoMockData]
    public void Relative_state_dir_resolves_from_config_dir(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var configDir = fs.CurrentDirectory().SubDirectory("config");

        env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);
        env.Set("RECYCLARR_STATE_DIR", "../state");

        var paths = sut.CreateAppPaths();

        paths
            .StateDirectory.FullName.Should()
            .Be(fs.CurrentDirectory().SubDirectory("state").FullName);
    }

    [Test, AutoMockData]
    public void Config_dir_is_not_written_when_data_and_state_dirs_are_elsewhere(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var configDir = fs.CurrentDirectory().SubDirectory("config");

        env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);
        env.Set("RECYCLARR_DATA_DIR", fs.CurrentDirectory().SubDirectory("data").FullName);
        env.Set("RECYCLARR_STATE_DIR", fs.CurrentDirectory().SubDirectory("state").FullName);

        sut.CreateAppPaths();

        fs.AllDirectories.Should().NotContain(configDir.FullName);
    }

    [Test, AutoMockData]
    public void User_config_directories_are_not_created(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var configDir = fs.CurrentDirectory().SubDirectory("config");

        env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);

        var paths = sut.CreateAppPaths();

        fs.AllDirectories.Should()
            .NotContain([paths.YamlConfigDirectory.FullName, paths.YamlIncludeDirectory.FullName]);
    }

    [Test, AutoMockData]
    public void Deprecated_app_data_variable_throws_error(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        env.Set("RECYCLARR_APP_DATA", "/some/path");

        var act = () => sut.CreateAppPaths();

        act.Should()
            .Throw<EnvironmentException>()
            .WithMessage("*RECYCLARR_APP_DATA is no longer supported*");
    }

    [Test, AutoMockData]
    public void Relative_data_dir_resolves_from_config_dir(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var configPath = fs.CurrentDirectory().SubDirectory("config").FullName;

        env.Set("RECYCLARR_CONFIG_DIR", configPath);
        env.Set("RECYCLARR_DATA_DIR", "../data");

        var paths = sut.CreateAppPaths();

        // Path is normalized by file system, so /config/../data becomes /data
        var expectedDataPath = fs.CurrentDirectory().SubDirectory("data").FullName;
        paths.ServerLogDirectory.FullName.Should().StartWith(expectedDataPath);
    }

    [Test, AutoMockData]
    public void Use_only_data_dir_with_platform_default_for_config(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        var platformPath = fs.CurrentDirectory().SubDirectory("platform").SubDirectory("default");
        var dataPath = fs.CurrentDirectory().SubDirectory("data").FullName;

        folders.GetFolderPath(default, default).ReturnsForAnyArgs(platformPath.FullName);
        env.Set("RECYCLARR_DATA_DIR", dataPath);

        var paths = sut.CreateAppPaths();

        // Config uses platform default
        var expectedConfigPath = platformPath.SubDirectory("recyclarr").FullName;
        paths.ConfigDirectory.FullName.Should().Be(expectedConfigPath);
        paths.YamlConfigDirectory.FullName.Should().StartWith(expectedConfigPath);

        // Data uses specified directory
        paths.ServerLogDirectory.FullName.Should().StartWith(dataPath);
        paths.ResourceDirectory.FullName.Should().StartWith(dataPath);
    }

    [Test, AutoMockData]
    public void Platform_default_unavailable_throws_exception(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        TestEnvironment env,
        [Frozen] ISpecialFolders folders
    )
    {
        var sut = CreateSut(fs, env, folders);
        folders.GetFolderPath(default, default).ReturnsForAnyArgs(string.Empty);

        var act = () => sut.CreateAppPaths();

        act.Should()
            .Throw<EnvironmentException>()
            .WithMessage("*Unable to find or create the default app data directory*");
    }
}
