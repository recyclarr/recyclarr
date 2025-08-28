using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.Repo;

namespace Recyclarr.TrashGuide;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public record TemplateEntry(string Id, string Template, bool Hidden = false);

public record TemplatesData
{
    public IReadOnlyCollection<TemplateEntry> Radarr { get; [UsedImplicitly] init; } =
        Array.Empty<TemplateEntry>();
    public IReadOnlyCollection<TemplateEntry> Sonarr { get; [UsedImplicitly] init; } =
        Array.Empty<TemplateEntry>();
}

public record TemplatePath
{
    public required string Id { get; init; }
    public required IFileInfo TemplateFile { get; init; }
    public required SupportedServices Service { get; init; }
    public bool Hidden { get; init; }
}

public class ConfigTemplateGuideService(IConfigTemplatesRepo repo) : IConfigTemplateGuideService
{
    private IReadOnlyCollection<TemplatePath>? _templateData;
    private IReadOnlyCollection<TemplatePath>? _includeData;

    public IReadOnlyCollection<TemplatePath> GetTemplateData()
    {
        return _templateData ??= LoadTemplateData("templates.json");
    }

    public IReadOnlyCollection<TemplatePath> GetIncludeData()
    {
        return _includeData ??= LoadTemplateData("includes.json");
    }

    private List<TemplatePath> LoadTemplateData(string templateFileName)
    {
        var templatesPath = repo.Path.File(templateFileName);
        if (!templatesPath.Exists)
        {
            throw new InvalidDataException(
                $"Recyclarr templates.json does not exist: {templatesPath}"
            );
        }

        var templates = Deserialize(templatesPath);

        return templates
            .Radarr.Select(x => NewTemplatePath(x, SupportedServices.Radarr))
            .Concat(templates.Sonarr.Select(x => NewTemplatePath(x, SupportedServices.Sonarr)))
            .ToList();

        TemplatePath NewTemplatePath(TemplateEntry entry, SupportedServices service)
        {
            return new TemplatePath
            {
                Id = entry.Id,
                TemplateFile = repo.Path.File(entry.Template),
                Service = service,
                Hidden = entry.Hidden,
            };
        }
    }

    private static TemplatesData Deserialize(IFileInfo jsonFile)
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
