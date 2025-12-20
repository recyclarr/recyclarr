using System.Text.Json;
using Recyclarr.Cache;
using Recyclarr.Json;

namespace Recyclarr.Core.Tests.Cache;

internal sealed class TrashIdMappingTest
{
    private static readonly JsonSerializerOptions JsonSettings =
        GlobalJsonSerializerSettings.Recyclarr;

    [Test]
    public void Deserialize_from_legacy_format_with_custom_format_fields()
    {
        const string legacyJson = """
            {
              "trash_id": "abc123",
              "custom_format_name": "AMZN",
              "custom_format_id": 42
            }
            """;

        var result = JsonSerializer.Deserialize<TrashIdMapping>(legacyJson, JsonSettings);

        result.Should().BeEquivalentTo(new TrashIdMapping("abc123", "AMZN", 42));
    }

    [Test]
    public void Deserialize_from_current_format_with_name_and_service_id()
    {
        const string currentJson = """
            {
              "trash_id": "abc123",
              "name": "AMZN",
              "service_id": 42
            }
            """;

        var result = JsonSerializer.Deserialize<TrashIdMapping>(currentJson, JsonSettings);

        result.Should().BeEquivalentTo(new TrashIdMapping("abc123", "AMZN", 42));
    }

    [Test]
    public void Serialize_outputs_current_format_only()
    {
        var mapping = new TrashIdMapping("abc123", "AMZN", 42);

        var json = JsonSerializer.Serialize(mapping, JsonSettings);

        json.Should().Contain("\"name\"").And.Contain("\"service_id\"");
        json.Should().NotContain("custom_format_name").And.NotContain("custom_format_id");
    }
}
