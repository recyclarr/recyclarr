using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Autofac;
using Recyclarr.Config.Models;
using Recyclarr.TestLibrary.Autofac;

namespace Recyclarr.Core.Tests.ServarrApi;

// Resolves gateways with the production Refit clients and serializer settings, backed by canned
// HTTP responses, so tests observe the JSON a gateway actually sends to Sonarr or Radarr.
internal abstract class ServarrHttpTestFixture : CoreIntegrationTestFixture
{
    private readonly StubServarrHandler _handler = new();

    protected IReadOnlyList<RecordedRequest> Requests => _handler.Requests;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterMockFor<IHttpClientFactory>(m =>
            m.CreateClient(default!)
                .ReturnsForAnyArgs(_ => new HttpClient(_handler, disposeHandler: false))
        );
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _handler.Dispose();
        }
    }

    protected void Respond(HttpMethod method, string path, string json)
    {
        _handler.Responses[(method, path)] = json;
    }

    protected T ResolveGateway<T>(ServiceConfiguration config)
        where T : notnull
    {
        config.BaseUrl = new Uri("http://servarr.test");
        return ResolveWithConfig<T>(config).Entry;
    }

    protected JsonNode SentBody(HttpMethod method, string path)
    {
        var body = Requests.Single(r => r.Method == method && r.Path == path).Body;
        // non-null: every recorded write request in these tests carries a JSON body
        return JsonNode.Parse(body!)!;
    }

    internal sealed record RecordedRequest(HttpMethod Method, string Path, string? Body);

    private sealed class StubServarrHandler : HttpMessageHandler
    {
        public Dictionary<(HttpMethod, string), string> Responses { get; } = [];
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            // non-null: HttpClient always sets the request URI from BaseAddress
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(request.Method, path, body));

            var json = Responses.GetValueOrDefault((request.Method, path), body ?? "{}");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }
}
