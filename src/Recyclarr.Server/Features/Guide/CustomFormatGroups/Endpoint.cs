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
        new(
            group.TrashId,
            group.Name,
            string.Equals(group.Default, "true", StringComparison.OrdinalIgnoreCase),
            [
                .. group
                    .CustomFormats.OrderBy(cf => cf.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(cf => new GuideCustomFormatGroupMemberResponse(
                        cf.TrashId,
                        cf.Name,
                        cf.Required,
                        cf.Default
                    )),
            ],
            [.. group.QualityProfiles.Include.Keys.Order(StringComparer.OrdinalIgnoreCase)]
        );
}
