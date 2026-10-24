using System.Text.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Recyclarr.Server.Api.Auth;
using Recyclarr.Server.Api.OpenApi;
using Scalar.AspNetCore;

namespace Recyclarr.Server.Api;

/// <summary>
/// The HTTP API under <c>/api</c>: FastEndpoints, wire JSON, API key authentication (ADR-027), and
/// the OpenAPI document with its Scalar reference page.
/// </summary>
internal static class ServerApi
{
    extension(IServiceCollection services)
    {
        public void AddServerApi()
        {
            // IResult responses (TypedResults.Json), which endpoints use for Problem Details with
            // extension members, serialize with ASP.NET's options rather than FastEndpoints'.
            services.ConfigureHttpJsonOptions(o => ConfigureWireJson(o.SerializerOptions));

            services
                .AddFastEndpoints(o =>
                {
                    // Endpoints only ever live in this assembly. Auto-discovery reflects over every
                    // loaded assembly, which throws when the host process has also loaded one whose
                    // dependencies cannot be resolved (FastEndpoints#121).
                    o.DisableAutoDiscovery = true;
                    o.Assemblies = [typeof(CompositionRoot).Assembly];
                })
                .OpenApiDocument(ConfigureDocument);

            // FastEndpoints requires authentication on every endpoint that does not opt out.
            services
                .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationHandler.SchemeName,
                    configureOptions: null
                );
            services.AddAuthorization();
        }
    }

    extension(WebApplication app)
    {
        public void UseServerApi()
        {
            app.UseFastEndpoints(c =>
            {
                c.Versioning.Prefix = "api/v";
                c.Versioning.PrependToRoute = true;
                // FastEndpoints' own ProblemDetails is sealed, so endpoints cannot add RFC 9457
                // extension members to it (FastEndpoints#637). ASP.NET's type can be subclassed
                // with typed members.
                c.Errors.ProducesMetadataType = typeof(HttpValidationProblemDetails);
                c.Errors.ResponseBuilder = ErrorResponses.Build;
                ConfigureWireJson(c.Serializer.Options);
                c.Endpoints.NameGenerator = OperationIds.Generate;
            });
            app.MapOpenApi();
            app.MapScalarApiReference();
        }
    }

    private static void ConfigureWireJson(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.RespectNullableAnnotations = true;
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    private static void ConfigureDocument(DocumentOptions o)
    {
        o.MaxEndpointVersion = 1;
        o.DocumentName = "v1";
        o.Title = "Recyclarr API";
        o.Version = "v1";
        o.ShortSchemaNames = true;
        o.EnableJWTBearerAuth = false;

        // No BearerFormat: the server API key is an opaque token, not a JWT.
        o.AddAuth(
            ApiKeyAuthenticationHandler.SchemeName,
            new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer" }
        );
        o.TagDescriptions = t =>
        {
            t["Guide"] = "Read-only TRaSH Guides data, scoped to one service.";
            t["Instances"] = "Direct calls to a configured Sonarr or Radarr instance.";
            t["Sync"] = "Sync jobs, their progress and results, and the built-in sync schedule.";
        };

        // Endpoints tag themselves explicitly; path-segment tagging would add a redundant "Api"
        // tag derived from the /api/v1 route prefix.
        o.AutoTagPathSegmentIndex = 0;
        o.ConfigureOpenApi = options =>
        {
            options.CreateSchemaReferenceId = PolymorphicSchemaIds.Wrap(
                options.CreateSchemaReferenceId
            );
            options.AddDocumentTransformer(DescribeDocument);
            options.AddDocumentTransformer(DocumentUnauthorizedResponses);
            options.AddDocumentTransformer<DiscriminatorDocumentTransformer>();
            options.AddSchemaTransformer<EnumDescriptionSchemaTransformer>();
        };
    }

    // Conceptual prose (overview, job lifecycle, authentication) lives in the wiki.
    private static Task DescribeDocument(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct
    )
    {
        document.Info.Description =
            "HTTP API of the Recyclarr server, which syncs TRaSH Guides to Sonarr and Radarr.";
        return Task.CompletedTask;
    }

    // FastEndpoints declares 401 with only a reason phrase, and not at all on endpoints that clear
    // default responses. Everything under /api requires the key (ADR-027).
    private static Task DocumentUnauthorizedResponses(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct
    )
    {
        var operations = document
            .Paths.Where(p => p.Key.StartsWith("/api/", StringComparison.Ordinal))
            .SelectMany(p => p.Value.Operations?.Values ?? Enumerable.Empty<OpenApiOperation>());

        foreach (var operation in operations)
        {
            operation.Responses ??= [];
            operation.Responses["401"] = new OpenApiResponse
            {
                Description =
                    "The request has no valid server API key in its Authorization header.",
            };
        }

        return Task.CompletedTask;
    }
}
