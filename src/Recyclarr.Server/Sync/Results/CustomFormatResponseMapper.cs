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
        ResultValueMapper.EnsureAllMapped(
            "Custom Format delta",
            result.Deltas.Count,
            creates.Count,
            updates.Count,
            deletes.Count
        );

        return new CustomFormatPipelineResponse
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = result.Outcomes.Select(OutcomeResponseMapper.Map).ToList(),
            Creates = creates,
            Updates = updates,
            Deletes = deletes,
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };
    }

    private static CustomFormatUpdateResponse MapUpdate(CustomFormatUpdateDelta delta)
    {
        var response = new CustomFormatUpdateResponse
        {
            Identity = MapIdentity(delta.Identity),
            SelectionProvenance = MapSelection(delta.SelectionProvenance),
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
}
