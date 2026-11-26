using System.IO.Abstractions;
using Recyclarr.Platform;
using Recyclarr.TestLibrary;

namespace Recyclarr.Core.Tests.Platform;

internal sealed class DefaultAppDataSetupTest
{
    private readonly MockFileSystem _fs = new();
    private readonly TestEnvironment _env = new();
    private readonly ISpecialFolders _folders = Substitute.For<ISpecialFolders>();

    private DefaultAppDataSetup CreateSut()
    {
        return new DefaultAppDataSetup(
            _env.Configuration,
            _fs,
            new ConfigDirectoryLocator(_env.Configuration, _folders, _fs)
        );
    }

    [Test]
    public void Initialize_using_default_path()
    {
        var sut = CreateSut();

        var basePath = _fs.CurrentDirectory().SubDirectory("base").SubDirectory("path");

        _folders.GetFolderPath(default, default).ReturnsForAnyArgs(basePath.FullName);

        var paths = sut.CreateAppPaths();

        paths.ConfigDirectory.FullName.Should().Be(basePath.SubDirectory("recyclarr").FullName);
    }

    [Test]
    public void Creation_uses_correct_behavior()
    {
        var sut = CreateSut();
        var appDataPath = _fs.CurrentDirectory().SubDirectory("override").SubDirectory("path");
        var basePath = _fs.CurrentDirectory().SubDirectory("base").SubDirectory("path");

        _folders.GetFolderPath(default, default).ReturnsForAnyArgs(basePath.FullName);

        sut.CreateAppPaths();

        _fs.AllDirectories.Should().NotContain(appDataPath.FullName);
    }

    [Test]
    public void Use_config_dir_environment_variable()
    {
        var sut = CreateSut();
        var expectedPath = _fs.CurrentDirectory()
            .SubDirectory("config")
            .SubDirectory("path")
            .FullName;

        _env.Set("RECYCLARR_CONFIG_DIR", expectedPath);

        var paths = sut.CreateAppPaths();

        paths.ConfigDirectory.FullName.Should().Be(expectedPath);
        paths.YamlConfigDirectory.FullName.Should().StartWith(expectedPath);
    }

    [Test]
    public void Use_data_dir_environment_variable()
    {
        var sut = CreateSut();
        var configPath = _fs.CurrentDirectory().SubDirectory("config").FullName;
        var dataPath = _fs.CurrentDirectory().SubDirectory("data").FullName;

        _env.Set("RECYCLARR_CONFIG_DIR", configPath);
        _env.Set("RECYCLARR_DATA_DIR", dataPath);

        var paths = sut.CreateAppPaths();

        paths
            .ServerLogDirectory.FullName.Should()
            .Be(_fs.CurrentDirectory().SubDirectory("data", "logs", "server").FullName);
        paths.ResourceDirectory.FullName.Should().StartWith(dataPath);
    }

    [Test]
    public void Data_dir_defaults_to_config_dir_when_unset()
    {
        var sut = CreateSut();
        var configPath = _fs.CurrentDirectory().SubDirectory("config").FullName;

        _env.Set("RECYCLARR_CONFIG_DIR", configPath);

        var paths = sut.CreateAppPaths();

        // Both config and data directories should be under the same root
        paths.YamlConfigDirectory.FullName.Should().StartWith(configPath);
        paths.ServerLogDirectory.FullName.Should().StartWith(configPath);
        paths.ResourceDirectory.FullName.Should().StartWith(configPath);
    }

    [Test]
    public void State_dir_defaults_under_config_dir_when_unset()
    {
        var sut = CreateSut();
        var configDir = _fs.CurrentDirectory().SubDirectory("config");

        _env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);

        var paths = sut.CreateAppPaths();

        paths.StateDirectory.FullName.Should().Be(configDir.SubDirectory("state").FullName);
        _fs.AllDirectories.Should().Contain(paths.StateDirectory.FullName);
    }

    [Test]
    public void Use_state_dir_environment_variable()
    {
        var sut = CreateSut();
        var configDir = _fs.CurrentDirectory().SubDirectory("config");
        var dataDir = _fs.CurrentDirectory().SubDirectory("data");
        var stateDir = _fs.CurrentDirectory().SubDirectory("state");

        _env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);
        _env.Set("RECYCLARR_DATA_DIR", dataDir.FullName);
        _env.Set("RECYCLARR_STATE_DIR", stateDir.FullName);

        var paths = sut.CreateAppPaths();

        paths.StateDirectory.FullName.Should().Be(stateDir.FullName);
        _fs.AllDirectories.Should().Contain(stateDir.FullName);
    }

    [Test]
    public void Relative_state_dir_resolves_from_config_dir()
    {
        var sut = CreateSut();
        var configDir = _fs.CurrentDirectory().SubDirectory("config");

        _env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);
        _env.Set("RECYCLARR_STATE_DIR", "../state");

        var paths = sut.CreateAppPaths();

        paths
            .StateDirectory.FullName.Should()
            .Be(_fs.CurrentDirectory().SubDirectory("state").FullName);
    }

    [Test]
    public void Config_dir_is_not_written_when_data_and_state_dirs_are_elsewhere()
    {
        var sut = CreateSut();
        var configDir = _fs.CurrentDirectory().SubDirectory("config");

        _env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);
        _env.Set("RECYCLARR_DATA_DIR", _fs.CurrentDirectory().SubDirectory("data").FullName);
        _env.Set("RECYCLARR_STATE_DIR", _fs.CurrentDirectory().SubDirectory("state").FullName);

        sut.CreateAppPaths();

        _fs.AllDirectories.Should().NotContain(configDir.FullName);
    }

    [Test]
    public void User_config_directories_are_not_created()
    {
        var sut = CreateSut();
        var configDir = _fs.CurrentDirectory().SubDirectory("config");

        _env.Set("RECYCLARR_CONFIG_DIR", configDir.FullName);

        var paths = sut.CreateAppPaths();

        _fs.AllDirectories.Should()
            .NotContain([paths.YamlConfigDirectory.FullName, paths.YamlIncludeDirectory.FullName]);
    }

    [Test]
    public void Deprecated_app_data_variable_throws_error()
    {
        var sut = CreateSut();
        _env.Set("RECYCLARR_APP_DATA", "/some/path");

        var act = () => sut.CreateAppPaths();

        act.Should()
            .Throw<EnvironmentException>()
            .WithMessage("*RECYCLARR_APP_DATA is no longer supported*");
    }

    [Test]
    public void Relative_data_dir_resolves_from_config_dir()
    {
        var sut = CreateSut();
        var configPath = _fs.CurrentDirectory().SubDirectory("config").FullName;

        _env.Set("RECYCLARR_CONFIG_DIR", configPath);
        _env.Set("RECYCLARR_DATA_DIR", "../data");

        var paths = sut.CreateAppPaths();

        // Path is normalized by file system, so /config/../data becomes /data
        var expectedDataPath = _fs.CurrentDirectory().SubDirectory("data").FullName;
        paths.ServerLogDirectory.FullName.Should().StartWith(expectedDataPath);
    }

    [Test]
    public void Use_only_data_dir_with_platform_default_for_config()
    {
        var sut = CreateSut();
        var platformPath = _fs.CurrentDirectory().SubDirectory("platform").SubDirectory("default");
        var dataPath = _fs.CurrentDirectory().SubDirectory("data").FullName;

        _folders.GetFolderPath(default, default).ReturnsForAnyArgs(platformPath.FullName);
        _env.Set("RECYCLARR_DATA_DIR", dataPath);

        var paths = sut.CreateAppPaths();

        // Config uses platform default
        var expectedConfigPath = platformPath.SubDirectory("recyclarr").FullName;
        paths.ConfigDirectory.FullName.Should().Be(expectedConfigPath);
        paths.YamlConfigDirectory.FullName.Should().StartWith(expectedConfigPath);

        // Data uses specified directory
        paths.ServerLogDirectory.FullName.Should().StartWith(dataPath);
        paths.ResourceDirectory.FullName.Should().StartWith(dataPath);
    }

    [Test]
    public void Platform_default_unavailable_throws_exception()
    {
        var sut = CreateSut();
        _folders.GetFolderPath(default, default).ReturnsForAnyArgs(string.Empty);

        var act = () => sut.CreateAppPaths();

        act.Should()
            .Throw<EnvironmentException>()
            .WithMessage("*Unable to find or create the default app data directory*");
    }
}
