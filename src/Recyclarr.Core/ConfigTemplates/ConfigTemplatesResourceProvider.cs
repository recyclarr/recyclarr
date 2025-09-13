using System.IO.Abstractions;
using System.Text.Json;
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

internal abstract class ConfigTemplatesResourceProvider
    : IConfigTemplatesResourceProvider,
        IConfigIncludesResourceProvider
{
    private readonly Lazy<List<IDirectoryInfo>> _repositoryPaths;

    protected ConfigTemplatesResourceProvider()
    {
        _repositoryPaths = new Lazy<List<IDirectoryInfo>>(() => GetSourceDirectories().ToList());
    }

    public string Name => "Config Templates";

    protected abstract IEnumerable<IDirectoryInfo> GetSourceDirectories();

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
