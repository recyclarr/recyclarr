using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Git;
using Recyclarr.Json;
using Recyclarr.TrashGuide;

namespace Recyclarr.ConfigTemplates;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public record TemplateEntry(string Id, string Template, bool Hidden = false);

public record TemplatesData
{
    public IReadOnlyCollection<TemplateEntry> Radarr { get; init; } = [];
    public IReadOnlyCollection<TemplateEntry> Sonarr { get; init; } = [];
}

public class ConfigTemplatesGitRepository(IGitRepositoryService gitRepositoryService)
    : IConfigTemplatesResourceProvider,
        IConfigIncludesResourceProvider
{
    private readonly Lazy<List<IDirectoryInfo>> _repositoryPaths = new(() =>
    {
        if (!gitRepositoryService.IsInitialized)
        {
            throw new InvalidOperationException(
                "GitRepositoryService must be initialized before accessing Config Template repositories."
            );
        }

        return gitRepositoryService.GetRepositoriesOfType("config-templates").ToList();
    });

    public string Name => "Config Templates";

    public string GetSourceDescription()
    {
        var repoCount = _repositoryPaths.Value.Count;
        return $"Config Template Repositories ({repoCount})";
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
            .Value.SelectMany(repoPath => LoadTemplateDataFromRepo(repoPath, templateFileName))
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
