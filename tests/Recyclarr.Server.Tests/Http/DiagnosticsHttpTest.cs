using System.Text.Json;
using System.Text.Json.Serialization;
using Recyclarr.Server.Features.Diagnostics.List;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Server.TestLibrary;

namespace Recyclarr.Server.Tests.Http;

internal sealed class DiagnosticsHttpTest : ServerHttpFixture
{
    private static readonly Uri Diagnostics = new("/api/v1/diagnostics", UriKind.Relative);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static Task<ListDiagnosticsResponse?> List(HttpClient client) =>
        client.GetFromJsonAsync<ListDiagnosticsResponse>(Diagnostics, JsonOptions);

    [Test]
    public async Task Server_without_problems_lists_no_diagnostics()
    {
        using var client = CreateClient();

        var response = await List(client);

        response!.Diagnostics.Should().BeEmpty();
    }

    [Test]
    public async Task Removed_v8_variable_is_listed_on_every_request()
    {
        Env.Set("CRON_SCHEDULE", "0 4 * * *");
        using var client = CreateClient();

        var first = await List(client);
        var second = await List(client);

        var diagnostic = first!.Diagnostics.Should().ContainSingle().Which;
        diagnostic.Id.Should().Be("environmentVariableRemoved:CRON_SCHEDULE");
        diagnostic.Code.Should().Be("environmentVariableRemoved");
        diagnostic.Severity.Should().Be(OutcomeSeverity.Warning);
        diagnostic.Message.Should().Contain("CRON_SCHEDULE").And.Contain("server.schedule.cron");
        second!.Diagnostics.Should().ContainSingle().Which.Id.Should().Be(diagnostic.Id);
    }
}
