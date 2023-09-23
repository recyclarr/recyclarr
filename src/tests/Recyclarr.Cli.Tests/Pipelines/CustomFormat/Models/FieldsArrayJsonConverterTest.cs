using System.Text.Json;
using Recyclarr.Json;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.Models;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class FieldsArrayJsonConverterTest
{
    [Test]
    public void Read_multiple_as_array()
    {
        const string json =
            """
            {
              "fields": [
                {
                  "order": 0,
                  "name": "min",
                  "label": "Minimum Size",
                  "unit": "GB",
                  "helpText": "Release must be greater than this size",
                  "value": 25,
                  "type": "number",
                  "advanced": false
                },
                {
                  "order": 1,
                  "name": "max",
                  "label": "Maximum Size",
                  "unit": "GB",
                  "helpText": "Release must be less than or equal to this size",
                  "value": 40,
                  "type": "number",
                  "advanced": false
                }
              ]
            }
            """;

        var result =
            JsonSerializer.Deserialize<CustomFormatSpecificationData>(json, GlobalJsonSerializerSettings.Services);

        result!.Fields.Should().BeEquivalentTo(new[]
        {
            new CustomFormatFieldData {Value = 25},
            new CustomFormatFieldData {Value = 40}
        });
    }

    [Test]
    public void Read_single_as_array()
    {
        const string json =
            """
            {
              "fields": {
                "order": 0,
                "name": "min",
                "label": "Minimum Size",
                "unit": "GB",
                "helpText": "Release must be greater than this size",
                "value": "25",
                "type": "number",
                "advanced": false
              }
            }
            """;
        var result =
            JsonSerializer.Deserialize<CustomFormatSpecificationData>(json, GlobalJsonSerializerSettings.Services);

        result!.Fields.Should().BeEquivalentTo(new[]
        {
            new CustomFormatFieldData {Value = "25"}
        });
    }

    [Test]
    public void Read_throws_on_unsupported_token_type()
    {
        const string json =
            """
            {
              "fields": 0
            }
            """;

        var act = () => JsonSerializer.Deserialize<CustomFormatSpecificationData>(
            json, GlobalJsonSerializerSettings.Services);

        act.Should().Throw<JsonException>();
    }
}
