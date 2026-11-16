using FastEndpoints;
using Recyclarr.Server.Diagnostics;

namespace Recyclarr.Server.Features.Diagnostics.List;

internal sealed class Endpoint(ServerDiagnostics diagnostics)
    : EndpointWithoutRequest<ListDiagnosticsResponse>
{
    public override void Configure()
    {
        Get("/diagnostics");

        Description(b => b.WithTags("Diagnostics"));
        Summary(s =>
        {
            s.Summary = "List current server diagnostics";
            s.Description =
                "Returns the problems the server has now, such as deprecated settings, an invalid "
                + "configuration change, or a guide update that was held back. Each one stays in "
                + "the list until the server sees it is fixed. Reading the list changes nothing. "
                + "The list is empty when the server has no problems.";
            s.Responses[200] = "The current diagnostics, errors first.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.ResponseAsync(new ListDiagnosticsResponse(diagnostics.Current), 200, ct);
    }
}
