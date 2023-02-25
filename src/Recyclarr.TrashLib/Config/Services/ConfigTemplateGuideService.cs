using System.Collections.ObjectModel;
using System.IO.Abstractions;
using JetBrains.Annotations;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Startup;

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
    private readonly IRepoMetadataBuilder _metadataBuilder;
    private readonly IAppPaths _paths;
    private readonly Lazy<IReadOnlyCollection<TemplatePath>> _templateData;

    public ConfigTemplateGuideService(
        IRepoMetadataBuilder metadataBuilder,
        IAppPaths paths)
    {
        _metadataBuilder = metadataBuilder;
        _paths = paths;
        _templateData = new Lazy<IReadOnlyCollection<TemplatePath>>(LoadTemplateData);
    }

    private IReadOnlyCollection<TemplatePath> LoadTemplateData()
    {
        var metadata = _metadataBuilder.GetMetadata();

        var templatesPath = _paths.RepoDirectory.SubDir(metadata.Recyclarr.Templates);
        if (!templatesPath.Exists)
        {
            throw new InvalidDataException(
                $"Path to recyclarr templates does not exist: {metadata.Recyclarr.Templates}");
        }

        var templates = TrashRepoJsonParser.Deserialize<TemplatesData>(templatesPath.File("templates.json"));

        TemplatePath NewTemplatePath(TemplateEntry entry, SupportedServices service)
        {
            return new TemplatePath(service, entry.Id, templatesPath.File(entry.Template), entry.Hidden);
        }

        return templates.Radarr
            .Select(x => NewTemplatePath(x, SupportedServices.Radarr))
            .Concat(templates.Sonarr.Select(x => NewTemplatePath(x, SupportedServices.Sonarr)))
            .ToList();
    }

    public IReadOnlyCollection<TemplatePath> TemplateData => _templateData.Value;
}
