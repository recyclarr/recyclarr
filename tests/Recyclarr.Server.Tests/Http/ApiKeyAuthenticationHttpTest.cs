using System.IO.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using Recyclarr.Server.TestLibrary;

namespace Recyclarr.Server.Tests.Http;

/// <summary>
/// Everything under /api requires the server API key as a bearer token; everything outside it is
/// anonymous (ADR-027).
/// </summary>
internal sealed class ApiKeyAuthenticationHttpTest : ServerHttpFixture
{
    private static readonly Uri ApiRoute = new("/api/v1/guide/radarr/qualities", UriKind.Relative);

    [TestCase(null, TestName = "No Authorization header is rejected")]
    [TestCase("Bearer wrong-key", TestName = "Wrong key is rejected")]
    [TestCase("Basic " + ApiKey, TestName = "Key in a scheme other than Bearer is rejected")]
    public async Task Api_request_without_a_valid_key_is_rejected(string? authorization)
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = authorization is null
            ? null
            : AuthenticationHeaderValue.Parse(authorization);

        var response = await client.GetAsync(ApiRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.Should().ContainSingle(h => h.Scheme == "Bearer");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Test]
    public async Task Api_request_with_the_key_is_accepted()
    {
        using var client = CreateClient();

        var response = await client.GetAsync(ApiRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Key_file_with_a_trailing_line_break_is_accepted()
    {
        UseKeyFile(ApiKey + "\n");
        using var client = CreateClient();

        var response = await client.GetAsync(ApiRoute);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Rotated_key_file_applies_without_a_restart()
    {
        var file = UseKeyFile(ApiKey);
        using var client = CreateClient();
        const string rotatedKey = "rotated-server-api-key-0123456789abcdef";

        file.WriteAllText(rotatedKey);
        var withOldKey = await client.GetAsync(ApiRoute);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            rotatedKey
        );
        var withNewKey = await client.GetAsync(ApiRoute);

        withOldKey.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        withNewKey.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task Key_file_removed_after_startup_rejects_requests()
    {
        var file = UseKeyFile(ApiKey);
        using var client = CreateClient();

        file.Delete();
        var response = await client.GetAsync(ApiRoute);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private IFileInfo UseKeyFile(string content)
    {
        var file = Fs.CurrentDirectory().File("api-key");
        file.WriteAllText(content);
        Env.GetEnvironmentVariable("RECYCLARR_API_KEY").Returns((string?)null);
        Env.GetEnvironmentVariable("RECYCLARR_API_KEY_FILE").Returns(file.FullName);
        return file;
    }

    [TestCase("/openapi/v1.json")]
    [TestCase("/scalar")]
    public async Task Routes_outside_api_need_no_key(string route)
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync(new Uri(route, UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
