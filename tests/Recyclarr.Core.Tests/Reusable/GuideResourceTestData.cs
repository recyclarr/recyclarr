using System.IO.Abstractions;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Core.Tests.Reusable;

internal sealed class GuideResourceTestData(MockFileSystem fs, ResourceRegistry<IFileInfo> registry)
{
    public void AddCustomFormats(params (string Name, string TrashId)[] customFormats)
    {
        foreach (var (name, trashId) in customFormats)
        {
            var resource = NewCf.RadarrData(name, trashId);
            var file = fs.CurrentDirectory()
                .SubDirectory("guide", "radarr", "cf")
                .File($"{trashId}.json");
            fs.AddJsonFile(file, resource, GlobalJsonSerializerSettings.Guide);
            registry.Register<RadarrCustomFormatResource>([file]);
        }
    }

    public void AddQualityProfile(
        string trashId,
        string name,
        params (string Name, bool Allowed, string[]? Items)[] qualities
    )
    {
        var resource = new RadarrQualityProfileResource
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
        Add("quality-profiles", trashId, resource);
    }

    public void AddQualityProfileWithFormatItems(
        string trashId,
        string name,
        string trashScoreSet,
        IReadOnlyDictionary<string, string> formatItems
    )
    {
        var resource = new RadarrQualityProfileResource
        {
            TrashId = trashId,
            Name = name,
            TrashScoreSet = trashScoreSet,
            FormatItems = formatItems,
        };
        Add("quality-profiles", trashId, resource);
    }

    public void AddCustomFormatWithScores(
        string name,
        string trashId,
        params (string ScoreSet, int Score)[] scores
    )
    {
        var resource = new RadarrCustomFormatResource
        {
            Name = name,
            TrashId = trashId,
            TrashScores = scores.ToDictionary(x => x.ScoreSet, x => x.Score),
        };
        Add("cf", trashId, resource);
    }

    public void AddCfGroup(
        string trashId,
        string name,
        IReadOnlyCollection<CfGroupCustomFormat> customFormats,
        IReadOnlyDictionary<string, string>? profileInclusions = null,
        bool isDefault = false
    )
    {
        var resource = new RadarrCfGroupResource
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
        Add("cf-groups", trashId, resource, GlobalJsonSerializerSettings.Metadata);
    }

    private void Add<T>(
        string resourceType,
        string trashId,
        T resource,
        System.Text.Json.JsonSerializerOptions? options = null
    )
        where T : class
    {
        var file = fs.CurrentDirectory()
            .SubDirectory("guide", "radarr", resourceType)
            .File($"{trashId}.json");
        fs.AddJsonFile(file, resource, options ?? GlobalJsonSerializerSettings.Guide);
        registry.Register<T>([file]);
    }
}
