using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Sync.Events;
using Recyclarr.TrashGuide.QualitySize;

namespace Recyclarr.Cli.Tests.Pipelines.Plan;

internal abstract class PlanBuilderTestBase : CliIntegrationFixture
{
    protected void SetupCustomFormatGuideData(params (string Name, string TrashId)[] cfs)
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        foreach (var (name, trashId) in cfs)
        {
            var cf = NewCf.RadarrData(name, trashId);
            var json = JsonSerializer.Serialize(cf, GlobalJsonSerializerSettings.Guide);
            var path = $"/guide/radarr/cf/{trashId}.json";
            Fs.AddFile(path, new MockFileData(json));
            registry.Register<RadarrCustomFormatResource>([Fs.FileInfo.New(path)]);
        }
    }

    protected void SetupQualitySizeGuideData(
        string type,
        params (string Name, decimal Min, decimal Max, decimal Preferred)[] qualities
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var qs = new RadarrQualitySizeResource
        {
            Type = type,
            Qualities = qualities
                .Select(q => new QualityItem
                {
                    Quality = q.Name,
                    Min = q.Min,
                    Max = q.Max,
                    Preferred = q.Preferred,
                })
                .ToList(),
        };
        var json = JsonSerializer.Serialize(qs, GlobalJsonSerializerSettings.Guide);
        var path = $"/guide/radarr/quality-size/{type}.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrQualitySizeResource>([Fs.FileInfo.New(path)]);
    }

    protected void SetupMediaNamingGuideData(
        IReadOnlyDictionary<string, string>? folderFormats = null,
        IReadOnlyDictionary<string, string>? fileFormats = null
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var naming = new RadarrMediaNamingResource
        {
            Folder = folderFormats ?? new Dictionary<string, string> { ["default"] = "{Movie}" },
            File = fileFormats ?? new Dictionary<string, string> { ["standard"] = "{Movie}.{ext}" },
        };
        var json = JsonSerializer.Serialize(naming, GlobalJsonSerializerSettings.Guide);
        var path = "/guide/radarr/naming/radarr-naming.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrMediaNamingResource>([Fs.FileInfo.New(path)]);
    }

    protected void SetupQualityProfileGuideData(
        string trashId,
        string name,
        params (string Name, bool Allowed, string[]? Items)[] qualities
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var qp = new RadarrQualityProfileResource
        {
            TrashId = trashId,
            Name = name,
            Items = qualities
                .Select(q => new QualityProfileQualityItem
                {
                    Name = q.Name,
                    Allowed = q.Allowed,
                    Items = q.Items ?? [],
                })
                .ToList(),
        };
        var json = JsonSerializer.Serialize(qp, GlobalJsonSerializerSettings.Guide);
        var path = $"/guide/radarr/quality-profiles/{trashId}.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrQualityProfileResource>([Fs.FileInfo.New(path)]);
    }

    protected void SetupQualityProfileWithFormatItems(
        string trashId,
        string name,
        string trashScoreSet,
        IReadOnlyDictionary<string, string> formatItems
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var qp = new RadarrQualityProfileResource
        {
            TrashId = trashId,
            Name = name,
            TrashScoreSet = trashScoreSet,
            FormatItems = formatItems,
        };
        var json = JsonSerializer.Serialize(qp, GlobalJsonSerializerSettings.Guide);
        var path = $"/guide/radarr/quality-profiles/{trashId}.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrQualityProfileResource>([Fs.FileInfo.New(path)]);
    }

    protected void SetupCustomFormatWithScores(
        string name,
        string trashId,
        params (string ScoreSet, int Score)[] scores
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var cf = new RadarrCustomFormatResource
        {
            Name = name,
            TrashId = trashId,
            TrashScores = scores.ToDictionary(x => x.ScoreSet, x => x.Score),
        };
        var json = JsonSerializer.Serialize(cf, GlobalJsonSerializerSettings.Guide);
        var path = $"/guide/radarr/cf/{trashId}.json";
        Fs.AddFile(path, new MockFileData(json));
        registry.Register<RadarrCustomFormatResource>([Fs.FileInfo.New(path)]);
    }

    protected void SetupCfGroupGuideData(
        string trashId,
        string name,
        IReadOnlyCollection<CfGroupCustomFormat> customFormats,
        IReadOnlyDictionary<string, string>? profileInclusions = null,
        bool isDefault = false
    )
    {
        var registry = Resolve<ResourceRegistry<IFileInfo>>();
        var group = new RadarrCfGroupResource
        {
            TrashId = trashId,
            Name = name,
            Default = isDefault ? "true" : "",
            CustomFormats = customFormats,
            QualityProfiles = new CfGroupProfiles
            {
                Include = profileInclusions ?? new Dictionary<string, string>(),
            },
        };
        var file = Fs.CurrentDirectory()
            .SubDirectory("guide", "radarr", "cf-groups")
            .File($"{trashId}.json");
        Fs.AddJsonFile(file, group, GlobalJsonSerializerSettings.Metadata);
        registry.Register<RadarrCfGroupResource>([file]);
    }

    protected static bool HasErrors(SyncEventStorage storage) =>
        storage.Diagnostics.Any(e => e.Type == DiagnosticType.Error);

    protected static IEnumerable<string> GetWarnings(SyncEventStorage storage) =>
        storage.Diagnostics.Where(e => e.Type == DiagnosticType.Warning).Select(e => e.Message);

    protected static IEnumerable<string> GetErrors(SyncEventStorage storage) =>
        storage.Diagnostics.Where(e => e.Type == DiagnosticType.Error).Select(e => e.Message);
}
