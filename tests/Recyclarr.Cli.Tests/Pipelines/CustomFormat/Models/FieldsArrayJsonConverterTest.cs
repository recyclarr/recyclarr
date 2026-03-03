using System.Text.Json;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Tests.Pipelines.CustomFormat.Models;

internal sealed class FieldsArrayJsonConverterTest
{
    [Test]
    public void Read_array_as_is()
    {
        const string json = """
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

        var result = JsonSerializer.Deserialize<CustomFormatSpecificationData>(
            json,
            GlobalJsonSerializerSettings.Services
        );

        result!.Fields.Should().BeEquivalentTo([NewCf.Field("min", 25), NewCf.Field("max", 40)]);
    }

    [Test]
    public void Convert_key_value_pairs_to_array()
    {
        const string json = """
            {
              "fields": {
                "value": 8,
                "exceptLanguage": false
              }
            }
            """;
        var result = JsonSerializer.Deserialize<CustomFormatSpecificationData>(
            json,
            GlobalJsonSerializerSettings.Services
        );

        result!
            .Fields.Should()
            .BeEquivalentTo([NewCf.Field("value", 8), NewCf.Field("exceptLanguage", false)]);
    }

    [Test]
    public void Read_throws_on_unsupported_token_type()
    {
        const string json = """
            {
              "fields": 0
            }
            """;

        var act = () =>
            JsonSerializer.Deserialize<CustomFormatSpecificationData>(
                json,
                GlobalJsonSerializerSettings.Services
            );

        act.Should().Throw<JsonException>();
    }
}
