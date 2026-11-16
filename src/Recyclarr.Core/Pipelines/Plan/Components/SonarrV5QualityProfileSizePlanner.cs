using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;

namespace Recyclarr.Pipelines.Plan.Components;

/// <summary>
/// Assigns quality sizes to each profile listed under <c>quality_profiles</c> (PDR-008). The first
/// available source wins: the profile's YAML <c>quality_definition</c>, then the guide profile's
/// <c>trash_quality_definition</c> reference, then the instance <c>quality_definition</c>.
/// Problems are reported per profile, including problems in the shared instance source.
/// </summary>
internal class SonarrV5QualityProfileSizePlanner(
    QualitySizeResolver resolver,
    QualitySizeResourceQuery guide,
    IServiceConfiguration config
) : IQualityProfileSizePlanner
{
    public IReadOnlyCollection<PlannedQualityProfile> Plan(
        IReadOnlyCollection<PlannedQualityProfile> profiles,
        PlanningContext context
    )
    {
        if (config.QualityDefinition is not null && !profiles.Any(x => x.IsManaged))
        {
            context.Add(new QualityDefinitionUnusedOutcome());
        }

        var instanceSizes = new Lazy<ResolvedSizes>(ResolveInstanceSizes);

        return
        [
            .. profiles.Select(profile =>
                profile.IsManaged ? AssignSizes(profile, instanceSizes, context) : profile
            ),
        ];
    }

    private PlannedQualityProfile AssignSizes(
        PlannedQualityProfile profile,
        Lazy<ResolvedSizes> instanceSizes,
        PlanningContext context
    )
    {
        void Report(SyncOutcome outcome) =>
            context.Add(new QualityProfileSizeOutcome(profile, outcome));

        var source = SelectProfileSource(profile, Report);
        if (source is ProfileSource.None)
        {
            return profile;
        }

        if (source is ProfileSource.Config { Definition: var definition })
        {
            return profile with { QualitySizes = resolver.Resolve(definition, Report) };
        }

        if (instanceSizes.Value is not { Sizes: { } sizes, Outcomes: var outcomes })
        {
            return profile;
        }

        foreach (var outcome in outcomes)
        {
            Report(outcome);
        }

        return profile with
        {
            QualitySizes = sizes,
        };
    }

    private ProfileSource SelectProfileSource(
        PlannedQualityProfile profile,
        Action<SyncOutcome> report
    )
    {
        if (profile.Config.QualityDefinition is { } yaml)
        {
            return new ProfileSource.Config(yaml);
        }

        var guideReference = profile.GuideResource?.TrashQualityDefinition;
        if (guideReference is null || guideReference.Count == 0)
        {
            return new ProfileSource.Instance();
        }

        if (guideReference.Count > 1)
        {
            report(new AmbiguousGuideQualityDefinitionOutcome(guideReference.Keys.ToList()));
            return new ProfileSource.None();
        }

        var (label, guideTrashId) = guideReference.First();
        var guideSizes = guide
            .Get(config.ServiceType)
            .LastOrDefault(x => x.TrashId.EqualsIgnoreCase(guideTrashId));

        if (guideSizes is null)
        {
            report(new GuideQualityDefinitionNotFoundOutcome(label, guideTrashId));
            return new ProfileSource.None();
        }

        return new ProfileSource.Config(new QualityDefinitionConfig { Type = guideSizes.Type });
    }

    private ResolvedSizes ResolveInstanceSizes()
    {
        if (config.QualityDefinition is not { } definition)
        {
            return new ResolvedSizes(null, []);
        }

        var outcomes = new List<SyncOutcome>();
        var sizes = resolver.Resolve(definition, outcomes.Add);
        return new ResolvedSizes(sizes, outcomes);
    }

    private sealed record ResolvedSizes(
        PlannedQualitySizes? Sizes,
        IReadOnlyList<SyncOutcome> Outcomes
    );

    private abstract record ProfileSource
    {
        public sealed record Config(QualityDefinitionConfig Definition) : ProfileSource;

        public sealed record Instance : ProfileSource;

        public sealed record None : ProfileSource;
    }
}
