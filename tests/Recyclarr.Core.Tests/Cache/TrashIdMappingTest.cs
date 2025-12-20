using System.Text.Json;
using Recyclarr.Cache;
using Recyclarr.Json;

namespace Recyclarr.Core.Tests.Cache;

internal sealed class TrashIdMappingTest
{
    [Test]
    public void Serialize_outputs_current_format_only()
    {
        var mapping = new TrashIdMapping("abc123", "AMZN", 42);

        var json = JsonSerializer.Serialize(mapping, GlobalJsonSerializerSettings.Recyclarr);

        json.Should().Contain("\"name\"").And.Contain("\"service_id\"");
        json.Should().NotContain("custom_format_name").And.NotContain("custom_format_id");
    }
}
