using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.Server.Resources;

/// <summary>
/// Holds a resource lease for every guide request (<c>/api/v{n}/guide/...</c>) and binds its guide
/// data to the request scope, so a refresh cannot change provider files while the request reads
/// them. Guide endpoints need no lease code of their own.
/// </summary>
internal sealed class GuideLeaseMiddleware(RequestDelegate next, ResourceCoordinator resources)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsGuideRequest(context.Request.Path))
        {
            await next(context);
            return;
        }

        using var lease = await resources.AcquireReadAsync(context.RequestAborted);
        context.RequestServices.GetRequiredService<ResourceScope>().Bind(lease.Snapshot.Guide);
        await next(context);
    }

    private static bool IsGuideRequest(PathString path)
    {
        var segments = (path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 3
            && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase)
            && segments[1].StartsWith('v')
            && segments[2].Equals("guide", StringComparison.OrdinalIgnoreCase);
    }
}
