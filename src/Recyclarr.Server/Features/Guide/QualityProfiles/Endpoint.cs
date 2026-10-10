using FastEndpoints;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Server.Features.Guide.QualityProfiles;

internal sealed class Endpoint(QualityProfileResourceQuery profiles)
    : Endpoint<GuideRequest, ListGuideQualityProfilesResponse>
{
    public override void Configure()
    {
        Get("/guide/{service}/quality-profiles");
        Version(1);

        // FastEndpoints secures endpoints by default. No authentication scheme exists yet, so the
        // opt-out is explicit until API key auth lands (REC-153).
        AllowAnonymous();

        Description(b => b.Produces<ListGuideQualityProfilesResponse>().WithTags("Guide"));
        Summary(s =>
        {
            s.Summary = "List guide quality profiles";
            s.Description = "Lists the guide's quality profiles for one service, sorted by name.";
            s.Responses[200] = "The quality profiles.";
            s.Responses[400] = GuideRequest.InvalidServiceResponse;
        });
    }

    public override async Task HandleAsync(GuideRequest req, CancellationToken ct)
    {
        var items = profiles
            .Get(req.Service)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToSummary)
            .ToList();

        await Send.OkAsync(new ListGuideQualityProfilesResponse(items), ct);
    }

    private static GuideQualityProfileSummaryResponse ToSummary(QualityProfileResource profile) =>
        new()
        {
            TrashId = profile.TrashId,
            Name = profile.Name,
            TrashUrl = NullIfEmpty(profile.TrashUrl),
            ScoreSet = NullIfEmpty(profile.TrashScoreSet),
            AllowedQualities =
            [
                .. profile
                    .Items.Where(q => q.Allowed)
                    .Select(q => new GuideProfileQualityResponse
                    {
                        Name = q.Name,
                        Items = [.. q.Items],
                    }),
            ],
            CustomFormats = [.. profile.FormatItems.Keys.Order(StringComparer.OrdinalIgnoreCase)],
        };

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
