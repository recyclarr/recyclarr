using FastEndpoints;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Server.Features.Guide.CustomFormatGroups;

internal sealed class Endpoint(CfGroupResourceQuery groups)
    : Endpoint<GuideRequest, ListGuideCustomFormatGroupsResponse>
{
    public override void Configure()
    {
        Get("/guide/{service}/custom-format-groups");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b => b.Produces<ListGuideCustomFormatGroupsResponse>().WithTags("Guide"));
        Summary(s =>
        {
            s.Summary = "List guide custom format groups";
            s.Description =
                "Lists the guide's custom format groups for one service, sorted by name. Each "
                + "group lists its custom formats and the guide quality profiles it applies to.";
            s.Responses[200] = "The custom format groups.";
            s.Responses[400] = GuideRequest.InvalidServiceResponse;
        });
    }

    public override async Task HandleAsync(GuideRequest req, CancellationToken ct)
    {
        var items = groups
            .Get(req.Service)
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToList();

        await Send.OkAsync(new ListGuideCustomFormatGroupsResponse(items), ct);
    }

    private static GuideCustomFormatGroupSummaryResponse ToSummary(CfGroupResource group) =>
        new()
        {
            TrashId = group.TrashId,
            Name = group.Name,
            Default = string.Equals(group.Default, "true", StringComparison.OrdinalIgnoreCase),
            CustomFormats =
            [
                .. group
                    .CustomFormats.OrderBy(cf => cf.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(cf => new GuideCustomFormatGroupMemberResponse
                    {
                        TrashId = cf.TrashId,
                        Name = cf.Name,
                        Required = cf.Required,
                        Default = cf.Default,
                    }),
            ],
            QualityProfiles =
            [
                .. group.QualityProfiles.Include.Keys.Order(StringComparer.OrdinalIgnoreCase),
            ],
        };
}
