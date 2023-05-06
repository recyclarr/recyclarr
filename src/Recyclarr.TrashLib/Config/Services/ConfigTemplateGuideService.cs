using System.Collections.ObjectModel;
using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.TrashLib.Config.Services;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public record TemplateEntry(string Id, string Template, bool Hidden = false);

public record TemplatesData
{
    public ReadOnlyCollection<TemplateEntry> Radarr { get; [UsedImplicitly] init; } = new(Array.Empty<TemplateEntry>());
    public ReadOnlyCollection<TemplateEntry> Sonarr { get; [UsedImplicitly] init; } = new(Array.Empty<TemplateEntry>());
}

public record TemplatePath(SupportedServices Service, string Id, IFileInfo TemplateFile, bool Hidden);

public class ConfigTemplateGuideService : IConfigTemplateGuideService
{
    private readonly IConfigTemplatesRepo _repo;

    public ConfigTemplateGuideService(IConfigTemplatesRepo repo)
    {
        _repo = repo;
    }

    public IReadOnlyCollection<TemplatePath> LoadTemplateData()
    {
        var templatesPath = _repo.Path.File("templates.json");
        if (!templatesPath.Exists)
        {
            throw new InvalidDataException(
                $"Recyclarr templates.json does not exist: {templatesPath}");
        }

        var templates = TrashRepoJsonParser.Deserialize<TemplatesData>(templatesPath);

        TemplatePath NewTemplatePath(TemplateEntry entry, SupportedServices service)
        {
            return new TemplatePath(service, entry.Id, _repo.Path.File(entry.Template), entry.Hidden);
        }

        return templates.Radarr
            .Select(x => NewTemplatePath(x, SupportedServices.Radarr))
            .Concat(templates.Sonarr.Select(x => NewTemplatePath(x, SupportedServices.Sonarr)))
            .ToList();
    }
}
