using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using CliFx.Attributes;
using CliFx.Exceptions;
using FluentAssertions;
using JetBrains.Annotations;
using NUnit.Framework;
using Recyclarr.Command;
using Recyclarr.Config;
using Recyclarr.TestLibrary;
using TestLibrary.AutoFixture;
using TrashLib;
using TrashLib.Config.Services;
using TrashLib.Startup;

namespace Recyclarr.Tests.Command;

[UsedImplicitly]
public class TestConfiguration : ServiceConfiguration
{
    public string ServiceName { get; set; }
}

[Command]
[UsedImplicitly]
public class TestServiceCommand : ServiceCommand
{
    private readonly ConfigurationLoader<TestConfiguration> _loader;
    public override string Name => nameof(TestServiceCommand);

    public IEnumerable<TestConfiguration> LoadedConfigs { get; private set; } = Array.Empty<TestConfiguration>();

    public TestServiceCommand(ConfigurationLoader<TestConfiguration> loader)
    {
        _loader = loader;
    }

    public async Task Process(IServiceLocatorProxy container, string[] configSections)
    {
        await base.Process(container);

        LoadedConfigs = configSections.SelectMany(x =>
        {
            return _loader.LoadMany(Config, x).Select(y =>
            {
                y.ServiceName = x;
                return y;
            });
        });
    }
}

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ServiceCommandTest : IntegrationFixture
{
    private static string[] GetYamlPaths(IAppPaths paths)
    {
        return new[]
        {
            paths.ConfigPath.FullName,
            paths.ConfigsDirectory.File("b.yml").FullName,
            paths.ConfigsDirectory.File("c.yml").FullName
        };
    }

    [Test, AutoMockData]
    public async Task Use_configs_dir_and_file_if_no_cli_argument(
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        IServiceLocatorProxy container,
        TestServiceCommand sut)
    {
        var yamlPaths = GetYamlPaths(paths);

        foreach (var path in yamlPaths)
        {
            fs.AddFile(path, new MockFileData(""));
        }

        await sut.Process(container);

        sut.Config.Should().BeEquivalentTo(yamlPaths);
    }

    [Test, AutoMockData]
    public async Task Use_paths_from_cli_instead_of_configs_dir_and_file_if_argument_specified(
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        IServiceLocatorProxy container,
        TestServiceCommand sut)
    {
        var yamlPaths = GetYamlPaths(paths);

        foreach (var path in yamlPaths)
        {
            fs.AddFile(path, new MockFileData(""));
        }

        var manualConfig = fs.CurrentDirectory().File("manual-config.yml");
        fs.AddFile(manualConfig.FullName, new MockFileData(""));
        sut.Config = new[] {manualConfig.FullName};

        await sut.Process(container);

        sut.Config.Should().BeEquivalentTo(manualConfig.FullName);
    }

    [Test, AutoMockData]
    public async Task Non_existent_files_are_skipped(
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        IServiceLocatorProxy container,
        TestServiceCommand sut)
    {
        var yamlPaths = GetYamlPaths(paths);

        fs.AddFile(yamlPaths[0], new MockFileData(""));
        fs.AddFile(yamlPaths[1], new MockFileData(""));

        sut.Config = yamlPaths.Take(2).ToList();

        await sut.Process(container);

        sut.Config.Should().BeEquivalentTo(yamlPaths.Take(2));
    }

    [Test, AutoMockData]
    public async Task No_recyclarr_yml_when_not_exists(
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        IServiceLocatorProxy container,
        TestServiceCommand sut)
    {
        var testFile = paths.ConfigsDirectory.File("test.yml").FullName;
        fs.AddFile(testFile, new MockFileData(""));

        await sut.Process(container);

        sut.Config.Should().BeEquivalentTo(testFile);
    }

    [Test, AutoMockData]
    public async Task Only_add_recyclarr_yml_when_exists(
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        IServiceLocatorProxy container,
        TestServiceCommand sut)
    {
        fs.AddFile(paths.ConfigPath.FullName, new MockFileData(""));

        await sut.Process(container);

        sut.Config.Should().BeEquivalentTo(paths.ConfigPath.FullName);
    }

    [Test, AutoMockData]
    public async Task Throw_when_no_configs_found(
        [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        IServiceLocatorProxy container,
        TestServiceCommand sut)
    {
        var act = () => sut.Process(container);

        await act.Should().ThrowAsync<CommandException>();
    }

    private static string MakeYamlData(string service, string url)
    {
        return $@"
{service}:
  - base_url: {url}
    api_key: abc123
";
    }

    [Test]
    public async Task Correct_yaml_loaded_multi_config(
        // [Frozen(Matching.ImplementedInterfaces)] AppPaths paths,
        // [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        // IServiceLocatorProxy container,
        // TestServiceCommand sut
        )
    {
        var sut = Resolve<TestServiceCommand>();
        var paths = Resolve<IAppPaths>();

        var yamlPaths = GetYamlPaths(paths);
        var data = new[]
        {
            MakeYamlData("sonarr", "a"),
            MakeYamlData("radarr", "b") + MakeYamlData("sonarr", "c"),
            MakeYamlData("radarr", "d")
        };

        foreach (var (path, yaml) in yamlPaths.Zip(data))
        {
            Fs.AddFile(path, new MockFileData(yaml));
        }

        await sut.Process(ServiceLocator, new[] {"sonarr", "radarr"});

        sut.LoadedConfigs.Should().BeEquivalentTo(new[]
        {
            new {ServiceName = "sonarr", BaseUrl = "a"},
            new {ServiceName = "radarr", BaseUrl = "b"},
            new {ServiceName = "sonarr", BaseUrl = "c"},
            new {ServiceName = "radarr", BaseUrl = "d"}
        });
    }
}
