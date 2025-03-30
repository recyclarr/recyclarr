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
    GitRepositorySource config,
    IRepoUpdater repoUpdater,
    IAppPaths appPaths
) : IConfigTemplatesResourceProvider, IConfigIncludesResourceProvider
{
    private readonly IDirectoryInfo _repoPath = appPaths.ReposDirectory.SubDirectory(
        $"config-templates-{config.Name ?? "default"}"
    );

    public string Name => config.Name ?? "default";

    public async Task Initialize(CancellationToken token)
    {
        if (config.CloneUrl is null)
        {
            throw new InvalidOperationException(
                "GitRepositorySource must have CloneUrl configured"
            );
        }

        var repoSettings = new GitRepositorySettings
        {
            CloneUrl = config.CloneUrl,
            Branch = config.Reference ?? "master",
            Sha1 = null,
        };

        await repoUpdater.UpdateRepo(_repoPath, repoSettings, token);
    }

    public IReadOnlyCollection<TemplatePath> GetTemplates()
    {
        return LoadTemplateData("templates.json");
    }

    public IReadOnlyCollection<IncludePath> GetIncludes()
    {
        return LoadIncludeData("includes.json");
    }

    private List<TemplatePath> LoadTemplateData(string templateFileName)
    {
        var templatesPath = _repoPath.File(templateFileName);
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
                TemplateFile = _repoPath.File(entry.Template),
                Service = service,
                Hidden = entry.Hidden,
            };
        }
    }

    private List<IncludePath> LoadIncludeData(string includeFileName)
    {
        var includesPath = _repoPath.File(includeFileName);
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
                IncludeFile = _repoPath.File(entry.Template),
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
