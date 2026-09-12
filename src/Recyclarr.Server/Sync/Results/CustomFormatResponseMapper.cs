using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Server.Features.Sync.GetResults;
using Riok.Mapperly.Abstractions;

namespace Recyclarr.Server.Sync.Results;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class CustomFormatResponseMapper
{
    public static CustomFormatPipelineResponse ToResponse(CustomFormatPipelineResult result)
    {
        var creates = result.Deltas.OfType<CustomFormatCreateDelta>().Select(MapCreate).ToList();
        var updates = result.Deltas.OfType<CustomFormatUpdateDelta>().Select(MapUpdate).ToList();
        var deletes = result
            .Deltas.OfType<CustomFormatDeleteDelta>()
            .Select(x => MapIdentity(x.Identity))
            .ToList();
        var outcomes = MapOutcomes(result.Outcomes);
        ResultValueMapper.EnsureAllMapped(
            "Custom Format outcome",
            result.Outcomes.Count,
            outcomes.ReferenceMismatches.Count,
            outcomes.GroupReferenceMismatches.Count,
            outcomes.IncompatibleGroups.Count,
            outcomes.EmptyGroups.Count,
            outcomes.Adopted.Count,
            outcomes.AmbiguousMatches.Count,
            outcomes.StateConflicts.Count,
            outcomes.CreateRejected.Count,
            outcomes.UpdateRejected.Count,
            outcomes.DeleteRejected.Count
        );
        ResultValueMapper.EnsureAllMapped(
            "Custom Format delta",
            result.Deltas.Count,
            creates.Count,
            updates.Count,
            deletes.Count
        );

        return new CustomFormatPipelineResponse(
            ResultValueMapper.MapStatus(result.Status),
            outcomes,
            creates,
            updates,
            deletes
        )
        {
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    private static CustomFormatOutcomesResponse MapOutcomes(
        IReadOnlyList<CustomFormatOutcome> outcomes
    ) =>
        new()
        {
            ReferenceMismatches = outcomes
                .OfType<CustomFormatReferenceMismatchOutcome>()
                .Select(x => x.TrashId)
                .ToList(),
            GroupReferenceMismatches = outcomes
                .OfType<CustomFormatGroupReferenceMismatchOutcome>()
                .Select(x => x.TrashId)
                .ToList(),
            IncompatibleGroups = outcomes
                .OfType<IncompatibleCustomFormatGroupOutcome>()
                .Select(x => new TrashIdNameResponse(x.TrashId, x.Name))
                .ToList(),
            EmptyGroups = outcomes
                .OfType<EmptyCustomFormatGroupOutcome>()
                .Select(x => new TrashIdNameResponse(x.TrashId, x.Name))
                .ToList(),
            Adopted = outcomes.OfType<CustomFormatAdoptedOutcome>().Select(MapAdopted).ToList(),
            AmbiguousMatches = outcomes
                .OfType<CustomFormatAmbiguousMatchOutcome>()
                .Select(MapAmbiguousMatch)
                .ToList(),
            StateConflicts = outcomes
                .OfType<CustomFormatStateConflictOutcome>()
                .Select(MapStateConflict)
                .ToList(),
            CreateRejected = outcomes
                .OfType<CustomFormatCreateRejectedOutcome>()
                .Select(x => MapIdentity(x.Identity))
                .ToList(),
            UpdateRejected = outcomes
                .OfType<CustomFormatUpdateRejectedOutcome>()
                .Select(x => MapIdentity(x.Identity))
                .ToList(),
            DeleteRejected = outcomes
                .OfType<CustomFormatDeleteRejectedOutcome>()
                .Select(x => MapIdentity(x.Identity))
                .ToList(),
        };

    private static CustomFormatUpdateResponse MapUpdate(CustomFormatUpdateDelta delta)
    {
        var response = new CustomFormatUpdateResponse(
            MapIdentity(delta.Identity),
            MapSelection(delta.SelectionProvenance)
        )
        {
            Name = delta
                .Components.OfType<CustomFormatNameChanged>()
                .Select(x => ResultValueMapper.MapValue(x.Value))
                .SingleOrDefault(),
            IncludeWhenRenaming = delta
                .Components.OfType<CustomFormatIncludeWhenRenamingChanged>()
                .Select(x => ResultValueMapper.MapValue(x.Value))
                .SingleOrDefault(),
            SpecificationsAdded = delta
                .Components.OfType<CustomFormatSpecificationAdded>()
                .Select(x => x.Name)
                .ToList(),
            SpecificationsChanged = delta
                .Components.OfType<CustomFormatSpecificationChanged>()
                .Select(x => x.Name)
                .ToList(),
            SpecificationsRemoved = delta
                .Components.OfType<CustomFormatSpecificationRemoved>()
                .Select(x => x.Name)
                .ToList(),
        };
        ResultValueMapper.EnsureAllMapped(
            "Custom Format update component",
            delta.Components.Count,
            response.Name is null ? 0 : 1,
            response.IncludeWhenRenaming is null ? 0 : 1,
            response.SpecificationsAdded.Count,
            response.SpecificationsChanged.Count,
            response.SpecificationsRemoved.Count
        );
        return response;
    }

    private static partial CustomFormatCreateResponse MapCreate(CustomFormatCreateDelta source);

    private static partial TrashIdNameResponse MapIdentity(CustomFormatIdentity source);

    private static partial CustomFormatSelectionResponse MapSelection(
        CustomFormatSourceInfo source
    );

    private static partial NamedServiceResourceResponse MapServiceMatch(
        CustomFormatServiceMatch source
    );

    private static NamedIdentityResponse MapAdopted(CustomFormatAdoptedOutcome source) =>
        new(MapIdentity(source.Identity), source.ServiceId);

    private static CustomFormatAmbiguousMatchResponse MapAmbiguousMatch(
        CustomFormatAmbiguousMatchOutcome source
    ) => new(MapIdentity(source.Identity), source.ServiceMatches.Select(MapServiceMatch).ToList());

    private static CustomFormatStateConflictResponse MapStateConflict(
        CustomFormatStateConflictOutcome source
    ) => new(MapIdentity(source.Identity), MapIdentity(source.ManagedIdentity), source.ServiceId);
}
