using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using NSubstitute;
using Recyclarr.ConfigTemplates;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.TestLibrary;

/// <summary>
/// Builder utility for creating configured resource providers for testing.
/// Provides fluent API for setting up providers with controlled test data.
/// </summary>
public class ResourceProviderTestBuilder
{
    private readonly MockFileSystem _mockFileSystem;

    public ResourceProviderTestBuilder(MockFileSystem mockFileSystem)
    {
        _mockFileSystem = mockFileSystem;
    }

    public GitTrashGuidesResourceProvider CreateTrashGuidesProvider(
        string name = "test-trash-guides",
        Action<TrashGuidesTestDataBuilder>? configureData = null
    )
    {
        var dataBuilder = new TrashGuidesTestDataBuilder(_mockFileSystem, name);
        configureData?.Invoke(dataBuilder);

        var gitRepoSource = new GitRepositorySource
        {
            CloneUrl = new Uri("https://github.com/test/guides.git"),
            Name = name,
            Reference = "master",
        };

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings { TrashGuides = [gitRepoSource] });

        var repoUpdater = Substitute.For<IRepoUpdater>();
        var appPaths = Substitute.For<IAppPaths>();
        appPaths.ReposDirectory.Returns(_mockFileSystem.DirectoryInfo.New("/repos"));

        return new GitTrashGuidesResourceProvider(settings, repoUpdater, appPaths);
    }

    public GitConfigTemplatesResourceProvider CreateConfigTemplatesProvider(
        string name = "test-config-templates",
        Action<ConfigTemplatesTestDataBuilder>? configureData = null
    )
    {
        var dataBuilder = new ConfigTemplatesTestDataBuilder(_mockFileSystem, name);
        configureData?.Invoke(dataBuilder);

        var gitRepoSource = new GitRepositorySource
        {
            CloneUrl = new Uri("https://github.com/test/templates.git"),
            Name = name,
            Reference = "master",
        };

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings { ConfigTemplates = [gitRepoSource] });

        var repoUpdater = Substitute.For<IRepoUpdater>();
        var appPaths = Substitute.For<IAppPaths>();
        appPaths.ReposDirectory.Returns(_mockFileSystem.DirectoryInfo.New("/repos"));

        return new GitConfigTemplatesResourceProvider(settings, repoUpdater, appPaths);
    }
}

/// <summary>
/// Builder for creating trash guides test data with realistic structure.
/// </summary>
public class TrashGuidesTestDataBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly MockFileSystem _mockFileSystem;
    private readonly string _repoPath;

    public TrashGuidesTestDataBuilder(
        MockFileSystem mockFileSystem,
        string providerName = "test-provider"
    )
    {
        _mockFileSystem = mockFileSystem;
        _repoPath = $"/repos/trash-guides-{providerName}";
    }

    public string RepoPath => _repoPath;

    public TrashGuidesTestDataBuilder WithMetadata(
        Action<MetadataBuilder>? configureMetadata = null
    )
    {
        var metadataBuilder = new MetadataBuilder();
        configureMetadata?.Invoke(metadataBuilder);

        var metadata = metadataBuilder.Build();
        var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);

        // Ensure directory exists
        _mockFileSystem.AddDirectory(_repoPath);
        _mockFileSystem.AddFile($"{_repoPath}/metadata.json", new MockFileData(metadataJson));
        return this;
    }

    public TrashGuidesTestDataBuilder WithCustomFormat(
        SupportedServices service,
        string name,
        object? customFormatData = null
    )
    {
        var serviceDir = service.ToString().ToLowerInvariant();
        var cfPath = $"{_repoPath}/docs/json/{serviceDir}/cf/{name}.json";

        var defaultCf = new
        {
            name,
            includeCustomFormatWhenRenaming = true,
            specifications = Array.Empty<object>(),
        };

        var cfJson = JsonSerializer.Serialize(customFormatData ?? defaultCf, JsonOptions);
        _mockFileSystem.AddFile(cfPath, new MockFileData(cfJson));
        return this;
    }

    public TrashGuidesTestDataBuilder WithQualityDefinition(
        SupportedServices service,
        object? qualityData = null
    )
    {
        var serviceDir = service.ToString().ToLowerInvariant();
        var qualityPath = $"{_repoPath}/docs/json/{serviceDir}/quality-size/quality-size.json";

        var defaultQuality = new[]
        {
            new
            {
                quality = "HDTV-720p",
                min = 2.0,
                preferred = 32.0,
                max = 100.0,
            },
        };

        var qualityJson = JsonSerializer.Serialize(qualityData ?? defaultQuality, JsonOptions);
        _mockFileSystem.AddFile(qualityPath, new MockFileData(qualityJson));
        return this;
    }

    public TrashGuidesTestDataBuilder WithNaming(
        SupportedServices service,
        object? namingData = null
    )
    {
        var serviceDir = service.ToString().ToLowerInvariant();
        var namingPath = $"{_repoPath}/docs/json/{serviceDir}/naming/naming.json";

        var defaultNaming = new
        {
            movie_format = "{Movie Title} ({Release Year}) - {Quality Full}",
            folder_format = "{Movie Title} ({Release Year})",
        };

        var namingJson = JsonSerializer.Serialize(namingData ?? defaultNaming, JsonOptions);
        _mockFileSystem.AddFile(namingPath, new MockFileData(namingJson));
        return this;
    }
}

/// <summary>
/// Builder for creating config templates test data.
/// </summary>
public class ConfigTemplatesTestDataBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly MockFileSystem _mockFileSystem;
    private readonly string _repoPath;

    public ConfigTemplatesTestDataBuilder(
        MockFileSystem mockFileSystem,
        string providerName = "test-provider"
    )
    {
        _mockFileSystem = mockFileSystem;
        _repoPath = $"/repos/config-templates-{providerName}";
    }

    public string RepoPath => _repoPath;

    public ConfigTemplatesTestDataBuilder WithTemplates(params TemplateInfo[] templates)
    {
        // If no templates provided, create a default one for testing
        if (templates.Length == 0)
        {
            templates =
            [
                new TemplateInfo(
                    "test-template",
                    SupportedServices.Radarr,
                    "radarr/nested/deep/template.yml"
                ),
            ];
        }

        var radarrTemplates = templates
            .Where(t => t.Service == SupportedServices.Radarr)
            .Select(t => new
            {
                t.Id,
                Template = t.Path,
                Hidden = false,
            })
            .ToArray();

        var sonarrTemplates = templates
            .Where(t => t.Service == SupportedServices.Sonarr)
            .Select(t => new
            {
                t.Id,
                Template = t.Path,
                Hidden = false,
            })
            .ToArray();

        var templatesData = new { Radarr = radarrTemplates, Sonarr = sonarrTemplates };

        var templatesJson = JsonSerializer.Serialize(templatesData, JsonOptions);

        // Ensure directory exists
        _mockFileSystem.AddDirectory(_repoPath);
        _mockFileSystem.AddFile($"{_repoPath}/templates.json", new MockFileData(templatesJson));

        // Create the actual template files
        foreach (var template in templates)
        {
            var templateContent =
                $"# {template.Id} Configuration\nbase_url: https://example.com\napi_key: fake-key";
            _mockFileSystem.AddFile(
                $"{_repoPath}/{template.Path}",
                new MockFileData(templateContent)
            );
        }

        return this;
    }

    public ConfigTemplatesTestDataBuilder WithIncludes(
        params (string Id, SupportedServices Service)[] includes
    )
    {
        var radarrIncludes = includes
            .Where(i => i.Service == SupportedServices.Radarr)
            .Select(i => new
            {
                i.Id,
                Template = $"includes/{i.Id}.yml",
                Hidden = false,
            })
            .ToArray();

        var sonarrIncludes = includes
            .Where(i => i.Service == SupportedServices.Sonarr)
            .Select(i => new
            {
                i.Id,
                Template = $"includes/{i.Id}.yml",
                Hidden = false,
            })
            .ToArray();

        var includesData = new { Radarr = radarrIncludes, Sonarr = sonarrIncludes };

        var includesJson = JsonSerializer.Serialize(includesData, JsonOptions);

        // Ensure directory exists
        _mockFileSystem.AddDirectory(_repoPath);
        _mockFileSystem.AddFile($"{_repoPath}/includes.json", new MockFileData(includesJson));

        // Create the actual include files
        foreach (var (id, _) in includes)
        {
            var includeContent = $"# {id} Include\nsome_setting: value";
            _mockFileSystem.AddFile(
                $"{_repoPath}/includes/{id}.yml",
                new MockFileData(includeContent)
            );
        }

        return this;
    }
}

/// <summary>
/// Builder for metadata.json structure.
/// </summary>
public class MetadataBuilder
{
    private readonly Dictionary<string, ServiceMetadata> _jsonPaths = new();

    public MetadataBuilder WithService(
        SupportedServices service,
        Action<ServiceMetadataBuilder> configure
    )
    {
        var serviceBuilder = new ServiceMetadataBuilder();
        configure(serviceBuilder);
        _jsonPaths[service.ToString().ToLowerInvariant()] = serviceBuilder.Build();
        return this;
    }

    public object Build()
    {
        return new { json_paths = _jsonPaths };
    }
}

/// <summary>
/// Builder for service-specific metadata.
/// </summary>
public class ServiceMetadataBuilder
{
    private readonly List<string> _customFormats = new();
    private readonly List<string> _qualities = new();
    private readonly List<string> _naming = new();

    public ServiceMetadataBuilder WithCustomFormats(params string[] paths)
    {
        _customFormats.AddRange(paths);
        return this;
    }

    public ServiceMetadataBuilder WithQualities(params string[] paths)
    {
        _qualities.AddRange(paths);
        return this;
    }

    public ServiceMetadataBuilder WithNaming(params string[] paths)
    {
        _naming.AddRange(paths);
        return this;
    }

    public ServiceMetadata Build()
    {
        return new ServiceMetadata
        {
            CustomFormats = _customFormats,
            Qualities = _qualities,
            Naming = _naming,
        };
    }
}

public record ServiceMetadata
{
    public IReadOnlyList<string> CustomFormats { get; init; } = [];
    public IReadOnlyList<string> Qualities { get; init; } = [];
    public IReadOnlyList<string> Naming { get; init; } = [];
}

public record TemplateInfo(string Id, SupportedServices Service, string Path);
