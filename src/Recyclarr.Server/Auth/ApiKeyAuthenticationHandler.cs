using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Recyclarr.Server.Auth;

/// <summary>
/// Authenticates a request whose <c>Authorization</c> header carries the server API key as a
/// bearer token (RFC 6750). The key is opaque, not a JWT, so ASP.NET's JWT bearer handler does
/// not apply. FastEndpoints requires authentication on every endpoint that does not opt out, so
/// this scheme guards everything under <c>/api</c> (ADR-027).
/// </summary>
internal sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ServerApiKey apiKey
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (
            !header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            || header.Parameter is null
            || !apiKey.Matches(header.Parameter)
        )
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        var identity = new ClaimsIdentity(SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Answers with Problem Details like every other error (ADR-011) and names the expected
    /// scheme in <c>WWW-Authenticate</c>.
    /// </summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer";

        var problem = ErrorResponses.Apply(
            new HttpValidationProblemDetails
            {
                Detail = "Send the server API key as a bearer token in the Authorization header.",
            },
            Context,
            StatusCodes.Status401Unauthorized
        );

        return TypedResults
            .Json(
                problem,
                contentType: "application/problem+json",
                statusCode: StatusCodes.Status401Unauthorized
            )
            .ExecuteAsync(Context);
    }
}
