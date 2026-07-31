using FastEndpoints;

namespace Recyclarr.Server.Features.Health;

internal sealed class Endpoint : EndpointWithoutRequest<HealthResponse>
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
        Description(b => b.ExcludeFromDescription());
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        Response = new HealthResponse("healthy");
        return Task.CompletedTask;
    }
}
