---
name: http-server
description: >-
  Use when writing, editing, or reviewing FastEndpoints endpoint classes,
  request/response DTOs, endpoint versioning, OpenAPI spec generation or
  build-time spec export, API versioning strategy, Kestrel server embedding,
  or Recyclarr.Server project structure. Triggers on phrases like "add an
  endpoint", "FastEndpoints", "endpoint class", "API version", "OpenAPI spec",
  "Kestrel server", "API endpoint", "HTTP server", "release group", "serve
  command". Do NOT use for MVC controllers, Razor Pages, or Minimal API
  lambda-style endpoints.
---

# HTTP Server

Recyclarr.Server is a thin HTTP layer over Core. Endpoints receive requests, call through port
interfaces into Core, and return responses. No business logic in the server project.

## FastEndpoints

One class per endpoint, auto-discovered. Request/response DTOs co-located with the endpoint.

```csharp
public class GetWidget : Endpoint<GetWidgetRequest, WidgetResponse>
{
    public override void Configure()
    {
        Get("/api/widgets/{id}");
        Version(1);
    }

    public override async Task HandleAsync(GetWidgetRequest req, CancellationToken ct)
    {
        // Call through port interface, not direct domain access
        var widget = await widgetPort.Get(req.Id, ct);
        await SendAsync(widget);
    }
}
```

### Feature Slice Structure

```txt
Features/
  Widgets/
    GetAll/
      Endpoint.cs
      Models.cs        ← request/response DTOs
    Get/
      Endpoint.cs
      Models.cs
  Health/
    Endpoint.cs
    Models.cs
```

No `Mapper.cs` or `Data.cs`; that logic lives in Core.

### Versioning (Release Group Strategy)

Only the changed endpoint gets a new class in a `V2/` subfolder. Unchanged endpoints are
automatically included in higher-version specs via `MaxEndpointVersion`.

```txt
Features/
  Orders/
    Create/
      Endpoint.cs      ← v1
      Models.cs
      V2/
        Endpoint.cs    ← breaking change only
        Models.cs
```

```csharp
// v1 endpoint: Version(1) in Configure()
// v2 endpoint: Version(2) in Configure(), same route
```

OpenAPI documents per version:

```csharp
bld.Services
    .OpenApiDocument(o => { o.MaxEndpointVersion = 1; o.DocumentName = "v1"; o.Version = "v1"; })
    .OpenApiDocument(o => { o.MaxEndpointVersion = 2; o.DocumentName = "v2"; o.Version = "v2"; });
```

The v2 doc includes the latest version (<= 2) of every endpoint. Unchanged v1 endpoints appear in
both docs without re-registration.

## OpenAPI Spec Workflow

The spec is committed to source control at `src/Recyclarr.Server/Recyclarr.Server.json`. Build-time
generation outputs to the project directory via `OpenApiDocumentsDirectory`. The committed spec is
the Kiota client's input.

- Non-RID builds regenerate the spec (local dev, CI test step)
- RID builds skip spec generation (cross-compilation); the committed file is always present
- API version (`v1`) is independent from the application semver
- `Recyclarr.Client` has a `ProjectReference` on Server for build ordering (not assembly reference)

## Kestrel Embedding

`WebApplication.CreateSlimBuilder()` for minimal footprint. Autofac via
`UseServiceProviderFactory`. Two modes share the same startup:

- `recyclarr serve`: standalone, persistent, shuts down on SIGTERM
- Ephemeral: CLI spawns server process, `READY:{port}` stdout handshake, stdin lifeline
