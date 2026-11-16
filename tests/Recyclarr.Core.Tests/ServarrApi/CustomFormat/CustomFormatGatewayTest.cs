using System.Text.Json.Nodes;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Core.Tests.ServarrApi.CustomFormat;

[TestFixture("radarr", "/api/v3")]
[TestFixture("sonarr-v3", "/api/v3")]
[TestFixture("sonarr-v5", "/api/v5")]
internal sealed class CustomFormatGatewayTest(string api, string apiRoot) : ServarrHttpTestFixture
{
    private string CustomFormatPath => $"{apiRoot}/customformat";

    private ICustomFormatService ResolveSut() =>
        api switch
        {
            "radarr" => ResolveGateway<RadarrCustomFormatGateway>(NewConfig.Radarr()),
            "sonarr-v3" => ResolveGateway<SonarrV3CustomFormatGateway>(NewConfig.Sonarr()),
            "sonarr-v5" => ResolveGateway<SonarrV5CustomFormatGateway>(NewConfig.Sonarr()),
            _ => throw new ArgumentOutOfRangeException(nameof(api), api, null),
        };

    [Test]
    public async Task Fetch_maps_custom_formats_to_domain()
    {
        Respond(
            HttpMethod.Get,
            CustomFormatPath,
            """
            [
              {
                "id": 7,
                "name": "CF",
                "includeCustomFormatWhenRenaming": true,
                "specifications": [
                  {
                    "name": "x",
                    "implementation": "ReleaseTitleSpecification",
                    "negate": false,
                    "required": true,
                    "fields": []
                  }
                ]
              }
            ]
            """
        );

        var result = await ResolveSut().GetCustomFormats(CancellationToken.None);

        result
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    Id = 7,
                    Name = "CF",
                    IncludeCustomFormatWhenRenaming = true,
                    Specifications = new[]
                    {
                        new { Implementation = "ReleaseTitleSpecification", Required = true },
                    },
                }
            );
    }

    [Test]
    public async Task Update_sends_custom_format_to_its_id_with_schema_metadata()
    {
        RespondWithReleaseTitleSchema();

        await ResolveSut().UpdateCustomFormat(NewCf() with { Id = 7 }, CancellationToken.None);

        SentBody(HttpMethod.Put, $"{CustomFormatPath}/7")["specifications"]![0]![
            "implementationName"
        ]!
            .GetValue<string>()
            .Should()
            .Be("Release Title");
    }

    [Test]
    public async Task Create_starts_specification_from_service_schema()
    {
        RespondWithReleaseTitleSchema();

        await ResolveSut()
            .CreateCustomFormat(
                NewCf(
                    new CustomFormatFieldData { Name = "value", Value = FieldValue.From("x264") },
                    new CustomFormatFieldData { Name = "extra", Value = FieldValue.From(5) }
                ),
                CancellationToken.None
            );

        var spec = SentSpecification();
        spec["name"]!.GetValue<string>().Should().Be("x");
        spec["negate"]!.GetValue<bool>().Should().BeTrue();
        spec["implementationName"]!.GetValue<string>().Should().Be("Release Title");
        spec["infoLink"]!.GetValue<string>().Should().Be("https://wiki.example/cf");
        spec["futureSpecField"]!.GetValue<string>().Should().Be("keep");
        spec.AsObject().Should().NotContainKey("presets");

        var fields = spec["fields"]!.AsArray();
        fields
            .Select(x => x!["name"]!.GetValue<string>())
            .Should()
            .Equal("value", "schemaOnly", "extra");
        fields[0]!["label"]!.GetValue<string>().Should().Be("Regular Expression");
        fields[0]!["value"]!.GetValue<string>().Should().Be("x264");
        fields[1]!["value"]!.GetValue<string>().Should().Be("default");
        fields[2]!["value"]!.GetValue<int>().Should().Be(5);
    }

    [Test]
    public async Task Create_does_not_leak_values_between_custom_formats()
    {
        RespondWithReleaseTitleSchema();
        var sut = ResolveSut();

        await sut.CreateCustomFormat(
            NewCf(new CustomFormatFieldData { Name = "schemaOnly", Value = FieldValue.From("a") }),
            CancellationToken.None
        );
        await sut.CreateCustomFormat(NewCf(), CancellationToken.None);

        SentSpecification(postIndex: 1)["fields"]!
            .AsArray()
            .Should()
            .ContainSingle(x => x!["name"]!.GetValue<string>() == "schemaOnly")
            .Which!["value"]!
            .GetValue<string>()
            .Should()
            .Be("default");
    }

    [Test]
    public async Task Create_sends_guide_specification_when_schema_lacks_implementation()
    {
        Respond(HttpMethod.Get, $"{CustomFormatPath}/schema", "[]");

        await ResolveSut()
            .CreateCustomFormat(
                NewCf(
                    new CustomFormatFieldData { Name = "value", Value = FieldValue.From("x264") }
                ),
                CancellationToken.None
            );

        var spec = SentSpecification();
        spec.AsObject().Should().NotContainKey("implementationName");
        spec["fields"]!.AsArray().Should().ContainSingle().Which!["value"]!
            .GetValue<string>()
            .Should()
            .Be("x264");
    }

    private void RespondWithReleaseTitleSchema()
    {
        Respond(
            HttpMethod.Get,
            $"{CustomFormatPath}/schema",
            """
            [
              {
                "implementation": "ReleaseTitleSpecification",
                "implementationName": "Release Title",
                "infoLink": "https://wiki.example/cf",
                "negate": false,
                "required": false,
                "futureSpecField": "keep",
                "fields": [
                  { "name": "value", "label": "Regular Expression" },
                  { "name": "schemaOnly", "value": "default" }
                ],
                "presets": [{ "name": "preset" }]
              }
            ]
            """
        );
    }

    private JsonNode SentSpecification(int postIndex = 0)
    {
        var body = Requests
            .Where(r => r.Method == HttpMethod.Post && r.Path == CustomFormatPath)
            .ElementAt(postIndex)
            .Body;
        // non-null: every custom format POST carries a JSON body
        return JsonNode.Parse(body!)!["specifications"]!.AsArray().Single()!;
    }

    private static CustomFormatResource NewCf(params CustomFormatFieldData[] fields)
    {
        return new CustomFormatResource
        {
            Name = "CF",
            Specifications =
            [
                new CustomFormatSpecificationData
                {
                    Name = "x",
                    Implementation = "ReleaseTitleSpecification",
                    Negate = true,
                    Fields = fields,
                },
            ],
        };
    }
}
