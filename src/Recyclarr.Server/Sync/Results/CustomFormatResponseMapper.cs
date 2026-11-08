using Recyclarr.Pipelines.CustomFormat;
using Recyclarr.Server.Features.Sync.GetResults;
using Riok.Mapperly.Abstractions;

namespace Recyclarr.Server.Sync.Results;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class CustomFormatResponseMapper
{
    public static CustomFormatPipelineResponse ToResponse(CustomFormatPipelineResult result) =>
        new()
        {
            Status = ResultValueMapper.MapStatus(result.Status),
            Outcomes = result.Outcomes.Select(OutcomeResponseMapper.Map).ToList(),
            Items = result.Resources.Select(MapItem).ToList(),
            Deletes = result.Deletes.Select(MapIdentity).ToList(),
            BlockedBy = ResultValueMapper.MapBlockedBy(result.BlockedBy),
        };

    private static CustomFormatItemResponse MapItem(PlannedCustomFormatResource resource) =>
        new()
        {
            Action = ResultValueMapper.MapAction(resource.Action),
            Identity = MapIdentity(resource.Identity),
            SelectionProvenance = MapSelection(resource.SelectionProvenance),
            Name = ResultValueMapper.MapValue(resource.Name),
            IncludeWhenRenaming = ResultValueMapper.MapNullableValue(resource.IncludeWhenRenaming),
            Specifications = resource.Specifications.Select(MapSpecification).ToList(),
        };

    private static partial CustomFormatSpecificationResponse MapSpecification(
        CustomFormatSpecificationResult source
    );

    private static partial TrashIdNameResponse MapIdentity(CustomFormatIdentity source);

    private static partial CustomFormatSelectionResponse MapSelection(
        CustomFormatSourceInfo source
    );
}
