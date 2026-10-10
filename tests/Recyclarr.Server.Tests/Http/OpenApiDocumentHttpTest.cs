using System.Text.Json.Nodes;
using Recyclarr.Server.TestLibrary;

namespace Recyclarr.Server.Tests.Http;

internal sealed class OpenApiDocumentHttpTest : ServerHttpFixture
{
    [Test]
    public async Task Enum_value_descriptions_are_keyed_by_wire_value()
    {
        var document = await GetDocument();
        var description = document?["components"]?["schemas"]?["InstanceProgressStatusResponse"]?[
            "description"
        ]?.GetValue<string>();

        description
            .Should()
            .StartWith("Sync progress of one instance in a job.")
            .And.Contain("- `notRun`: The job stopped before this instance started.");
    }

    // The wiki API reference renders only what the document contains.
    [Test]
    public async Task Every_operation_parameter_response_and_tag_is_described()
    {
        var document = await GetDocument();
        var operations = document!["paths"]!
            .AsObject()
            .SelectMany(path =>
                path.Value!.AsObject().Select(op => (Name: $"{op.Key} {path.Key}", Op: op.Value!))
            )
            .ToList();

        var undescribed = operations
            .Where(x => IsBlank(x.Op["summary"]) || IsBlank(x.Op["description"]))
            .Select(x => x.Name)
            .Concat(
                operations.SelectMany(x =>
                    (x.Op["parameters"]?.AsArray() ?? [])
                        .Where(p => IsBlank(p!["description"]))
                        .Select(p => $"{x.Name} parameter {p!["name"]}")
                )
            )
            .Concat(
                operations.SelectMany(x =>
                    x.Op["responses"]!
                        .AsObject()
                        .Where(r => !IsSentence(r.Value!["description"]))
                        .Select(r => $"{x.Name} response {r.Key}")
                )
            )
            .Concat(
                (document["tags"]?.AsArray() ?? [])
                    .Where(t => IsBlank(t!["description"]))
                    .Select(t => $"tag {t!["name"]}")
            );

        undescribed.Should().BeEmpty();
    }

    private async Task<JsonNode?> GetDocument()
    {
        using var client = CreateClient();
        return JsonNode.Parse(
            await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative))
        );
    }

    private static bool IsBlank(JsonNode? node) =>
        string.IsNullOrWhiteSpace(node?.GetValue<string>());

    // FastEndpoints fills an undocumented response with a reason phrase such as "Success", never a
    // sentence that states the condition.
    private static bool IsSentence(JsonNode? node) =>
        node?.GetValue<string>().EndsWith('.') ?? false;
}
