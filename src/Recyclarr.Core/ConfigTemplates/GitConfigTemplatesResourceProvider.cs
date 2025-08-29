using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.ConfigTemplates;

public class GitConfigTemplatesResourceProvider(
    ISettings<ResourceProviderSettings> settings,
    IRepoUpdater repoUpdater,
    IAppPaths appPaths
) : IConfigTemplatesResourceProvider, IConfigIncludesResourceProvider
{
    private readonly Lazy<Task<Dictionary<string, ProcessedRepository>>> _repositories = new(() =>
        ProcessAllRepositoriesAsync(settings, repoUpdater, appPaths)
    );

    private static async Task<Dictionary<string, ProcessedRepository>> ProcessAllRepositoriesAsync(
        ISettings<ResourceProviderSettings> settings,
        IRepoUpdater repoUpdater,
        IAppPaths appPaths
    )
    {
        var results = new Dictionary<string, ProcessedRepository>();

        foreach (var gitRepo in settings.Value.ConfigTemplates.OfType<GitRepositorySource>())
        {
            var processed = await ProcessSingleRepository(gitRepo, repoUpdater, appPaths);
            results[gitRepo.Name ?? "default"] = processed;
        }

        return results;
    }

    private static async Task<ProcessedRepository> ProcessSingleRepository(
        GitRepositorySource config,
        IRepoUpdater repoUpdater,
        IAppPaths appPaths
    )
    {
        if (config.CloneUrl is null)
        {
            throw new InvalidOperationException(
                "GitRepositorySource must have CloneUrl configured"
            );
        }

        var repoPath = appPaths.ReposDirectory.SubDirectory(
            $"config-templates-{config.Name ?? "default"}"
        );

        var repoSettings = new GitRepositorySettings
        {
            CloneUrl = config.CloneUrl,
            Branch = config.Reference ?? "master",
            Sha1 = null,
        };

        await repoUpdater.UpdateRepo(repoPath, repoSettings, CancellationToken.None);

        return new ProcessedRepository(repoPath);
    }

    private record ProcessedRepository(IDirectoryInfo RepoPath);

    public string Name => "Git Config Templates Provider";

    public async Task Initialize(CancellationToken token)
    {
        // Process all repositories - this triggers the lazy initialization
        await _repositories.Value;
    }

    public IReadOnlyCollection<TemplatePath> GetTemplates()
    {
        var repos = _repositories.Value.GetAwaiter().GetResult();
        var allTemplates = new List<TemplatePath>();

        foreach (var (name, repo) in repos)
        {
            allTemplates.AddRange(LoadTemplateData(repo.RepoPath, "templates.json"));
        }

        return allTemplates;
    }

    public IReadOnlyCollection<IncludePath> GetIncludes()
    {
        var repos = _repositories.Value.GetAwaiter().GetResult();
        var allIncludes = new List<IncludePath>();

        foreach (var (name, repo) in repos)
        {
            allIncludes.AddRange(LoadIncludeData(repo.RepoPath, "includes.json"));
        }

        return allIncludes;
    }

    private static List<TemplatePath> LoadTemplateData(
        IDirectoryInfo repoPath,
        string templateFileName
    )
    {
        var templatesPath = repoPath.File(templateFileName);
        if (!templatesPath.Exists)
        {
            return [];
        }

        var templates = DeserializeTemplatesData(templatesPath);

        return templates
            .Radarr.Select(x => NewTemplatePath(x, SupportedServices.Radarr))
            .Concat(templates.Sonarr.Select(x => NewTemplatePath(x, SupportedServices.Sonarr)))
            .ToList();

        TemplatePath NewTemplatePath(TemplateEntry entry, SupportedServices service)
        {
            return new TemplatePath
            {
                Id = entry.Id,
                TemplateFile = repoPath.File(entry.Template),
                Service = service,
                Hidden = entry.Hidden,
            };
        }
    }

    private static List<IncludePath> LoadIncludeData(
        IDirectoryInfo repoPath,
        string includeFileName
    )
    {
        var includesPath = repoPath.File(includeFileName);
        if (!includesPath.Exists)
        {
            return [];
        }

        var includes = DeserializeTemplatesData(includesPath);

        return includes
            .Radarr.Select(x => NewIncludePath(x, SupportedServices.Radarr))
            .Concat(includes.Sonarr.Select(x => NewIncludePath(x, SupportedServices.Sonarr)))
            .ToList();

        IncludePath NewIncludePath(TemplateEntry entry, SupportedServices service)
        {
            return new IncludePath
            {
                Id = entry.Id,
                IncludeFile = repoPath.File(entry.Template),
                Service = service,
            };
        }
    }

    private static TemplatesData DeserializeTemplatesData(IFileInfo jsonFile)
    {
        using var stream = jsonFile.OpenRead();
        var obj = JsonSerializer.Deserialize<TemplatesData>(
            stream,
            GlobalJsonSerializerSettings.Recyclarr
        );
        if (obj is null)
        {
            throw new InvalidDataException($"Unable to deserialize {jsonFile}");
        }

        return obj;
    }

    private class GitRepositorySettings : IRepositorySettings
    {
        public Uri CloneUrl { get; init; } = null!;
        public string Branch { get; init; } = "master";
        public string? Sha1 { get; init; }
    }
}
