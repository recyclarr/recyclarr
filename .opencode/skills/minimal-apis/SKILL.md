---
name: minimal-apis
description: >-
  Use when writing, editing, or reviewing ASP.NET Core Minimal API endpoints,
  route handlers, route groups, endpoint filters, or API responses; configuring
  OpenAPI document generation or build-time spec export; implementing endpoint
  validation or problem details error handling; using Server-Sent Events for
  streaming; writing integration tests against API endpoints; embedding Kestrel
  in a CLI tool. Triggers on phrases like "add an endpoint", "minimal API",
  "route group", "endpoint filter", "typed results", "OpenAPI spec", "Kestrel
  server", "API endpoint", "HTTP server endpoints". Do NOT use for MVC
  controllers, Razor Pages, Blazor Server, or SignalR hubs.
---

# Minimal APIs (.NET 10)

Best practices for ASP.NET Core Minimal APIs targeting .NET 10. Minimal APIs are Microsoft's
recommended approach for new HTTP API projects, offering simplified syntax, better performance, and
reduced overhead compared to controller-based APIs.

## Core Principles

- Use `WebApplication.CreateSlimBuilder()` for embedded servers (excludes IIS, HTTPS dev cert,
  EventLog). Use `CreateBuilder()` only when the full feature set is needed.
- Organize endpoints into static extension method classes, one per feature area. Each class maps a
  `RouteGroupBuilder` with a shared prefix.
- Use `TypedResults` (not `Results`) for all return values. `TypedResults` provides compile-time
  type safety, automatic OpenAPI metadata, and testable concrete types.
- Use `Results<T1, T2, ...TN>` union return types to declare all possible responses. The compiler
  enforces that only declared result types are returned.
- Use `[AsParameters]` to group handler parameters into records or structs, reducing parameter
  bloat.

## Endpoint Organization

Structure endpoints as static classes with extension methods on `IEndpointRouteBuilder`. One class
per feature area, one file per class.

```csharp
// SyncEndpoints.cs
public static class SyncEndpoints
{
    public static RouteGroupBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync");

        group.MapPost("/", TriggerSync);
        group.MapGet("/jobs/{id}", GetJobStatus);

        return group;
    }

    private static async Task<Results<Accepted<JobReference>, ValidationProblem>> TriggerSync(
        [AsParameters] SyncRequest request,
        ISyncPort syncPort,
        CancellationToken ct)
    {
        // Handler implementation
    }

    private static async Task<Results<Ok<JobStatus>, NotFound>> GetJobStatus(
        Guid id,
        IJobTracker tracker)
    {
        // Handler implementation
    }
}
```

Register in `Program.cs` with one line per feature area:

```csharp
app.MapSyncEndpoints();
app.MapConfigEndpoints();
app.MapCustomFormatEndpoints();
```

### Route Groups

Use `MapGroup` for shared prefixes, filters, and metadata:

```csharp
var api = app.MapGroup("/api")
    .AddEndpointFilter<RequestLoggingFilter>()
    .ProducesProblem(StatusCodes.Status500InternalServerError);

api.MapGroup("/sync").MapSyncEndpoints();
api.MapGroup("/config").MapConfigEndpoints();
```

Groups support nesting. Filters and metadata applied to a group affect all endpoints in that group.

## TypedResults and Response Types

MUST use `TypedResults` over `Results` for all return values. `TypedResults` automatically provides
OpenAPI response metadata and returns strongly typed objects for unit testing.

```csharp
// Correct: TypedResults with union return type
private static async Task<Results<Ok<SyncResult>, NotFound, Accepted<JobReference>>> Handle(...)
{
    return condition switch
    {
        true => TypedResults.Ok(result),
        false => TypedResults.NotFound(),
        _ => TypedResults.Accepted($"/api/jobs/{jobId}", new JobReference(jobId))
    };
}
```

Available `TypedResults` factory methods (non-exhaustive):

| Method | Status | Use case |
| --- | --- | --- |
| `Ok<T>()` | 200 | Successful response with body |
| `Created<T>()` | 201 | Resource created |
| `Accepted<T>()` | 202 | Long-running operation started |
| `NoContent()` | 204 | Success, no body |
| `BadRequest<T>()` | 400 | Invalid request |
| `NotFound()` | 404 | Resource not found |
| `Problem()` | 500 | RFC 9457 problem details |
| `ValidationProblem()` | 400 | Validation errors |
| `InternalServerError()` | 500 | Server error |
| `ServerSentEvents()` | 200 | SSE stream |

## Parameter Binding

### `[AsParameters]` for grouped parameters

Group related parameters into a record or struct to reduce handler signature bloat:

```csharp
public record SyncRequest(
    [FromBody] SyncRequestBody Body,
    [FromQuery] bool? Preview,
    [FromHeader(Name = "X-Request-Id")] string? RequestId);

app.MapPost("/api/sync", async ([AsParameters] SyncRequest request, ISyncPort port) => ...);
```

### Binding sources

Parameters bind automatically based on HTTP method and type:

- **Route values**: `{id}` in the route template
- **Query string**: Primitive types for GET/HEAD/OPTIONS/DELETE
- **Body (JSON)**: Complex types for POST/PUT/PATCH
- **DI services**: Types registered in the DI container (no `[FromServices]` needed)
- **Special types**: `HttpContext`, `HttpRequest`, `HttpResponse`, `CancellationToken`,
  `ClaimsPrincipal` bind automatically

Use explicit attributes (`[FromRoute]`, `[FromQuery]`, `[FromHeader]`, `[FromBody]`, `[FromForm]`)
when the default binding source is not what you want.

## Endpoint Filters

Endpoint filters are the Minimal API equivalent of MVC action filters. They run before and after the
handler and can inspect/modify parameters and responses.

```csharp
// Filter as a class implementing IEndpointFilter
public class RequestLoggingFilter(ILogger<RequestLoggingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        logger.LogInformation("Request: {Method} {Path}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path);

        var result = await next(context);

        return result;
    }
}

// Apply to a group or individual endpoint
group.AddEndpointFilter<RequestLoggingFilter>();
```

Filters resolve dependencies from DI. Execution order follows registration order (FIFO before
handler, FILO after handler).

## Validation (.NET 10)

.NET 10 adds built-in validation support for Minimal APIs using `DataAnnotations`:

```csharp
builder.Services.AddValidation();
```

This automatically validates parameters annotated with `[Required]`, `[Range]`, `[StringLength]`,
etc. Validation failures return 400 Bad Request with problem details.

Works with both classes and records:

```csharp
public record CreateSyncRequest(
    [Required] string InstanceName,
    [Range(1, 3600)] int TimeoutSeconds);
```

Customize error responses via `IProblemDetailsService`. Disable validation on specific endpoints
with `.DisableValidation()`.

## OpenAPI

### Runtime document generation

```csharp
builder.Services.AddOpenApi();
// ...
app.MapOpenApi(); // Serves at /openapi/v1.json by default
```

The `Microsoft.AspNetCore.OpenApi` package generates OpenAPI 3.1 documents with JSON Schema 2020-12.
Supports both Minimal APIs and controllers.

### Build-time document generation

Add `Microsoft.Extensions.ApiDescription.Server` for build-time spec output:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
  <PackageReference Include="Microsoft.Extensions.ApiDescription.Server"
                    PrivateAssets="all" />
</ItemGroup>

<PropertyGroup>
  <OpenApiGenerateDocuments>true</OpenApiGenerateDocuments>
  <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/../openapi</OpenApiDocumentsDirectory>
</PropertyGroup>
```

Build-time generation launches the app's entry point with a mock server. Guard startup code that
requires live dependencies:

```csharp
if (Assembly.GetEntryAssembly()?.GetName().Name != "GetDocument.Insider")
{
    // Code that requires database connections, etc.
}
```

### XML doc comments in OpenAPI

.NET 10 supports pulling `///` XML doc comments into the OpenAPI document. Enable in the project
file:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

### OpenAPI YAML

.NET 10 supports serving OpenAPI in YAML format:

```csharp
app.MapOpenApi("/openapi/{documentName}.yaml");
```

### API UI with Scalar

Microsoft recommends Scalar over Swagger UI for interactive API documentation:

```csharp
using Scalar.AspNetCore;

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Available at /scalar
}
```

### OpenAPI document transformers

Customize the generated document with transformer APIs:

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "Recyclarr API";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});
```

## Server-Sent Events (.NET 10)

.NET 10 adds `TypedResults.ServerSentEvents` for streaming event data:

```csharp
app.MapGet("/api/sync/progress/{jobId}", (Guid jobId, IJobTracker tracker) =>
{
    async IAsyncEnumerable<SseItem<ProgressUpdate>> StreamProgress(
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var update in tracker.GetProgressStream(jobId, ct))
        {
            yield return SseItem.Create(update, eventType: "progress");
        }
    }

    return TypedResults.ServerSentEvents(StreamProgress());
});
```

The client receives events over a single HTTP connection with `Content-Type: text/event-stream`.

## Error Handling

### Problem Details (RFC 9457)

Register problem details service for consistent error responses:

```csharp
builder.Services.AddProblemDetails();
```

This ensures all error responses (including framework-generated 404s, 500s, etc.) follow the Problem
Details format.

### Exception handling

Use `UseExceptionHandler` middleware or `UseStatusCodePages` for global error handling:

```csharp
app.UseExceptionHandler(); // Uses IProblemDetailsService when registered
app.UseStatusCodePages();  // Produces Problem Details for empty error responses
```

## Testing

### Unit testing handlers

Static handler methods with `TypedResults` return types are directly testable:

```csharp
[Test]
public async Task GetJob_WhenNotFound_ReturnsNotFound()
{
    var tracker = Substitute.For<IJobTracker>();
    tracker.GetJob(Arg.Any<Guid>()).Returns((JobStatus?)null);

    var result = await SyncEndpoints.GetJobStatus(Guid.NewGuid(), tracker);

    result.Result.Should().BeOfType<NotFound>();
}
```

### Integration testing with WebApplicationFactory

```csharp
public class SyncApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SyncApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Test]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

Requires `<InternalsVisibleTo Include="TestProject" />` or `public partial class Program { }` in the
server project.

## Autofac Integration

For projects using Autofac as the DI container:

```csharp
var builder = WebApplication.CreateSlimBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule<CoreAutofacModule>();
});
```

## Embedded Kestrel Server

When embedding Kestrel in a CLI tool (like Recyclarr's HTTP server mode):

- Use `CreateSlimBuilder()` for minimal footprint
- Configure bind address and port via settings, not hardcoded
- Add a health endpoint (`/health`) for readiness checks
- Implement graceful shutdown via `IHostApplicationLifetime` or `app.StopAsync()`
- Share the `WebApplication` startup logic between persistent (`serve`) and ephemeral modes

```csharp
// Shared server startup
public static WebApplication BuildServer(string[] args, Action<ContainerBuilder>? configure = null)
{
    var builder = WebApplication.CreateSlimBuilder(args);

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>(cb =>
    {
        cb.RegisterModule<CoreAutofacModule>();
        configure?.Invoke(cb);
    });

    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.MapOpenApi();
    app.MapHealthChecks("/health");
    app.MapSyncEndpoints();

    return app;
}
```

## Key Microsoft Learn References

- [APIs overview](https://learn.microsoft.com/aspnet/core/fundamentals/apis?view=aspnetcore-10.0)
- [Minimal APIs quick
  reference](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0)
- [Parameter
  binding](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-10.0)
- [Responses and
  TypedResults](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0)
- [Endpoint
  filters](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters?view=aspnetcore-10.0)
- [OpenAPI document
  generation](https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0)
- [Error handling in
  APIs](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0)
- [What's new in .NET
  10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)
