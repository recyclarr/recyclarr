using System.IO.Abstractions;
using System.Text.Json;
using JetBrains.Annotations;
using Recyclarr.Json;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.TrashLib.Guide;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public record TemplateEntry(string Id, string Template, bool Hidden = false);

public record TemplatesData
{
    public IReadOnlyCollection<TemplateEntry> Radarr { get; [UsedImplicitly] init; } = Array.Empty<TemplateEntry>();
    public IReadOnlyCollection<TemplateEntry> Sonarr { get; [UsedImplicitly] init; } = Array.Empty<TemplateEntry>();
}

public record TemplatePath
{
    public required string Id { get; init; }
    public required IFileInfo TemplateFile { get; init; }
    public required SupportedServices Service { get; init; }
    public bool Hidden { get; init; }
}

public class ConfigTemplateGuideService : IConfigTemplateGuideService
{
    private readonly IConfigTemplatesRepo _repo;
    private IReadOnlyCollection<TemplatePath>? _templateData;
    private IReadOnlyCollection<TemplatePath>? _includeData;

    public ConfigTemplateGuideService(IConfigTemplatesRepo repo)
    {
        _repo = repo;
    }

    public IReadOnlyCollection<TemplatePath> GetTemplateData()
    {
        return _templateData ??= LoadTemplateData("templates.json");
    }

    public IReadOnlyCollection<TemplatePath> GetIncludeData()
    {
        return _includeData ??= LoadTemplateData("includes.json");
    }

    private IReadOnlyCollection<TemplatePath> LoadTemplateData(string templateFileName)
    {
        var templatesPath = _repo.Path.File(templateFileName);
        if (!templatesPath.Exists)
        {
            throw new InvalidDataException(
                $"Recyclarr templates.json does not exist: {templatesPath}");
        }

        var templates = Deserialize(templatesPath);

        return templates.Radarr
            .Select(x => NewTemplatePath(x, SupportedServices.Radarr))
            .Concat(templates.Sonarr.Select(x => NewTemplatePath(x, SupportedServices.Sonarr)))
            .ToList();

        TemplatePath NewTemplatePath(TemplateEntry entry, SupportedServices service)
        {
            return new TemplatePath
            {
                Id = entry.Id,
                TemplateFile = _repo.Path.File(entry.Template),
                Service = service,
                Hidden = entry.Hidden
            };
        }
    }

    private static TemplatesData Deserialize(IFileInfo jsonFile)
    {
        using var stream = jsonFile.OpenRead();
        var obj = JsonSerializer.Deserialize<TemplatesData>(stream, GlobalJsonSerializerSettings.Recyclarr);
        if (obj is null)
        {
            throw new InvalidDataException($"Unable to deserialize {jsonFile}");
        }

        return obj;
    }
}
