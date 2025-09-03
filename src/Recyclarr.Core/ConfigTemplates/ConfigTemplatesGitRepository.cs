using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ResourceProviders;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.ConfigTemplates;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public record TemplateEntry(string Id, string Template, bool Hidden = false);

public record TemplatesData
{
    public IReadOnlyCollection<TemplateEntry> Radarr { get; init; } = [];
    public IReadOnlyCollection<TemplateEntry> Sonarr { get; init; } = [];
}

public class ConfigTemplatesGitRepository(
    ISettings<ResourceProviderSettings> settings,
    IRepoUpdater repoUpdater,
    IAppPaths appPaths
) : IConfigTemplatesResourceProvider, IConfigIncludesResourceProvider
{
    private readonly List<IDirectoryInfo> _repositoryPaths = [];

    public string Name => "Git Config Templates Provider";

    public IDirectoryInfo RepoParentPath { get; } =
        appPaths.ReposDirectory.SubDirectory("config-templates");

    private async Task<IDirectoryInfo> ProcessSingleRepository(
        GitRepositorySource config,
        CancellationToken token
    )
    {
        var repoPath = RepoParentPath.SubDirectory(config.Name);
        await repoUpdater.UpdateRepo(repoPath, config, token);
        return repoPath;
    }

    public async Task Initialize(CancellationToken token)
    {
        // Clean up legacy Git repository if it exists
        LegacyRepositoryCleanup.CleanLegacyRepository(RepoParentPath);

        // Always include official config templates repository first
        var officialRepo = new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
            Reference = "master",
        };

        var allRepos = new[] { officialRepo }.Concat(
            settings.Value.ConfigTemplates.OfType<GitRepositorySource>()
        );

        foreach (var gitRepo in allRepos)
        {
            var repoPath = await ProcessSingleRepository(gitRepo, token);
            _repositoryPaths.Add(repoPath);
        }
    }

    public IReadOnlyCollection<TemplatePath> GetTemplates()
    {
        return LoadTemplateData("templates.json");
    }

    public IReadOnlyCollection<TemplatePath> GetIncludes()
    {
        return LoadTemplateData("includes.json");
    }

    private List<TemplatePath> LoadTemplateData(string templateFileName)
    {
        return _repositoryPaths
            .SelectMany(repoPath => LoadTemplateDataFromRepo(repoPath, templateFileName))
            .ToList();
    }

    private static List<TemplatePath> LoadTemplateDataFromRepo(
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
}
