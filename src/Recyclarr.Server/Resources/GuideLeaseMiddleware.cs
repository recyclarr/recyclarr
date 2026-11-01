using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Server.Resources;

/// <summary>
/// Holds a resource lease for every request that reads provider data (<c>/api/v{n}/guide/...</c>,
/// <c>/config-templates</c>, <c>/config-includes</c>) and binds its guide data to the request
/// scope, so a refresh cannot change provider files while the request reads them. Those endpoints
/// need no lease code of their own.
/// </summary>
internal sealed class GuideLeaseMiddleware(RequestDelegate next, ResourceCoordinator resources)
{
    private static readonly HashSet<string> LeasedResources = new(
        ["guide", "config-templates", "config-includes"],
        StringComparer.OrdinalIgnoreCase
    );

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ReadsProviderData(context.Request.Path))
        {
            await next(context);
            return;
        }

        using var lease = await resources.AcquireReadAsync(context.RequestAborted);
        context.RequestServices.GetRequiredService<ResourceScope>().Bind(lease.Snapshot.Guide);
        await next(context);
    }

    private static bool ReadsProviderData(PathString path)
    {
        var segments = (path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 3
            && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase)
            && segments[1].StartsWith('v')
            && LeasedResources.Contains(segments[2]);
    }
}
