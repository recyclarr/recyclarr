---
name: http-server
description: >-
  Use when writing, editing, or reviewing FastEndpoints endpoint classes,
  request/response DTOs, endpoint versioning, OpenAPI spec generation or
  build-time spec export, API versioning strategy, Kestrel server embedding,
  or Recyclarr.Server project structure; designing or reviewing API routes,
  URL structure, REST resources, or endpoint paths. Triggers on phrases like
  "add an endpoint", "FastEndpoints", "endpoint class", "API version",
  "OpenAPI spec", "Kestrel server", "API endpoint", "API route", "REST
  resource", "URL structure", "endpoint path", "HTTP server", "release
  group", "serve command". Do NOT use for MVC controllers, Razor Pages, or
  Minimal API lambda-style endpoints.
---

# HTTP Server

Recyclarr.Server is a thin HTTP layer over Core. Endpoints receive requests, call through port
interfaces into Core, and return responses. No business logic in the server project.

## Route Conventions

Rationale and the full resource model live in ADR-011; config representation in ADR-012. The rules:

```txt
/api/{version}/{namespace?}/{resource}[/{id}/{resource}[/{id}...]]
```

- Path versioning: `/api/v1/...`. API version is independent from application semver.
- Namespaces (`sync/`, `guide/{service}/`) group resources and NEVER resolve; `GET /api/v1/sync`
  404s. One namespace level max.
- `{service}` is a closed enum (`radarr|sonarr`), not a collection id.
- Collections are plural and id-addressable (`instances/{name}`). Singletons are singular with no id
  (`sync/schedule`). Test: can more than one exist?
- Kebab-case resource names (`custom-formats`).
- Instance names are globally unique (config validation enforces this); `/instances/{name}` relies
  on it.
- No DELETE with a request body; bulk deletes are per-item calls in a client loop.
- `/health` stays outside `/api`: unversioned, anonymous, excluded from the OpenAPI spec. Everything
  under `/api` requires the API key.
- All error responses use RFC 9457 Problem Details.
- Response DTOs are domain-shaped, never YAML-shaped. Secrets (e.g. `api_key`) are absent from DTOs,
  not masked.

## Long-Running Operations

Model as job resources, never verb endpoints (ADR-011):

- Trigger = create: `POST /api/v1/sync/jobs` returns 202 + `Location: /api/v1/sync/jobs/{id}`
- `GET .../jobs/{id}` returns 200 (done) or 202 (running) + `Retry-After`
- The snapshot is cumulative (per-instance, per-operation); a slow or skipped poll loses nothing
- Polling only; no SSE
- Large result payloads belong on a sub-resource (`GET .../jobs/{id}/results`), never on the polled
  job resource (ADR-014); DTOs are shaped for what consumers render, not mirrors of domain types
- "Is one running" = filter the jobs collection, never a singleton status resource

## FastEndpoints

One class per endpoint, auto-discovered. Request/response DTOs co-located with the endpoint.

```csharp
public class GetWidget : Endpoint<GetWidgetRequest, WidgetResponse>
{
    public override void Configure()
    {
        Get("/api/v1/widgets/{id}");
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
the input to Refitter, which generates the typed client (ADR-013).

- Non-RID builds regenerate the spec (local dev, CI test step)
- RID builds skip spec generation (cross-compilation); the committed file is always present
- API version (`v1`) is independent from the application semver
- `Recyclarr.Client` has a `ProjectReference` on Server for build ordering (not assembly reference)
- One `.refitter` config per API version, scoped by `includePathMatches` into a version namespace
  (`Recyclarr.Client.V1`), so the version stays out of the call chain

## Kestrel Embedding

`WebApplication.CreateSlimBuilder()` for minimal footprint. Autofac via `UseServiceProviderFactory`.
Two modes share the same startup:

- `recyclarr serve`: standalone, persistent, shuts down on SIGTERM
- Ephemeral: CLI spawns server process, `READY:{port}` stdout handshake, stdin lifeline
