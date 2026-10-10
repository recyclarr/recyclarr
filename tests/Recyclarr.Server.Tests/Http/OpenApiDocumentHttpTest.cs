using System.Text.Json.Nodes;
using Recyclarr.Server.TestLibrary;

namespace Recyclarr.Server.Tests.Http;

internal sealed class OpenApiDocumentHttpTest : ServerHttpFixture
{
    [Test]
    public async Task Enum_value_descriptions_are_keyed_by_wire_value()
    {
        using var client = CreateClient();

        var document = JsonNode.Parse(
            await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative))
        );
        var description = document?["components"]?["schemas"]?["InstanceProgressStatusResponse"]?[
            "description"
        ]?.GetValue<string>();

        description
            .Should()
            .StartWith("Sync progress of one instance in a job.")
            .And.Contain("- `notRun`: The job stopped before this instance started.");
    }
}
