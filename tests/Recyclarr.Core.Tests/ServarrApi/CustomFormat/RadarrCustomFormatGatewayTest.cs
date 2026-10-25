using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.Core.Tests.ServarrApi.CustomFormat;

internal sealed class RadarrCustomFormatGatewayTest
{
    [Test, AutoMockData]
    public async Task Create_starts_specification_from_service_schema(
        [Frozen] RadarrApi.ICustomFormatApi api,
        RadarrCustomFormatGateway sut
    )
    {
        ArrangeSchema(api, NewReleaseTitleSchema());

        await sut.CreateCustomFormat(
            NewCf(
                new CustomFormatFieldData { Name = "value", Value = FieldValue.From("x264") },
                new CustomFormatFieldData { Name = "extra", Value = FieldValue.From(5) }
            ),
            CancellationToken.None
        );

        SentSpecs(api)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    Name = "x",
                    Negate = true,
                    ImplementationName = "Release Title",
                    InfoLink = "https://wiki.example/cf",
                    Presets = (object?)null,
                    Fields = new object[]
                    {
                        new
                        {
                            Name = "value",
                            Label = "Regular Expression",
                            Value = "x264",
                        },
                        new { Name = "schemaOnly", Value = "default" },
                        new { Name = "extra", Value = 5 },
                    },
                }
            );
    }

    [Test, AutoMockData]
    public async Task Create_does_not_leak_values_between_custom_formats(
        [Frozen] RadarrApi.ICustomFormatApi api,
        RadarrCustomFormatGateway sut
    )
    {
        ArrangeSchema(api, NewReleaseTitleSchema());

        await sut.CreateCustomFormat(
            NewCf(new CustomFormatFieldData { Name = "schemaOnly", Value = FieldValue.From("a") }),
            CancellationToken.None
        );
        await sut.CreateCustomFormat(NewCf(), CancellationToken.None);

        SentSpecs(api, callIndex: 1)
            .Should()
            .ContainSingle()
            .Which.Fields.Should()
            .ContainSingle(x => x.Name == "schemaOnly")
            .Which.Value.Should()
            .Be("default");
    }

    [Test, AutoMockData]
    public async Task Create_sends_guide_specification_when_schema_lacks_implementation(
        [Frozen] RadarrApi.ICustomFormatApi api,
        RadarrCustomFormatGateway sut
    )
    {
        ArrangeSchema(api);

        await sut.CreateCustomFormat(
            NewCf(new CustomFormatFieldData { Name = "value", Value = FieldValue.From("x264") }),
            CancellationToken.None
        );

        SentSpecs(api)
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    Name = "x",
                    ImplementationName = (string?)null,
                    Fields = new[] { new { Name = "value", Value = "x264" } },
                }
            );
    }

    private static void ArrangeSchema(
        RadarrApi.ICustomFormatApi api,
        params RadarrApi.CustomFormatSpecificationSchema[] schemas
    )
    {
        api.Schema().ReturnsForAnyArgs(schemas);
        api.CustomformatPost(default!)
            .ReturnsForAnyArgs(
                new RadarrApi.CustomFormatResource
                {
                    Id = 1,
                    Name = "CF",
                    IncludeCustomFormatWhenRenaming = false,
                    Specifications = [],
                }
            );
    }

    private static RadarrApi.CustomFormatSpecificationSchema NewReleaseTitleSchema()
    {
        return new RadarrApi.CustomFormatSpecificationSchema
        {
            Implementation = "ReleaseTitleSpecification",
            ImplementationName = "Release Title",
            InfoLink = "https://wiki.example/cf",
            Negate = false,
            Required = false,
            Fields =
            [
                new RadarrApi.Field { Name = "value", Label = "Regular Expression" },
                new RadarrApi.Field { Name = "schemaOnly", Value = "default" },
            ],
            Presets = [new RadarrApi.CustomFormatSpecificationSchema { Name = "preset" }],
        };
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

    private static ICollection<RadarrApi.CustomFormatSpecificationSchema>? SentSpecs(
        RadarrApi.ICustomFormatApi api,
        int callIndex = 0
    )
    {
        var sent = (RadarrApi.CustomFormatResource)
            api.ReceivedCalls()
                .Where(c =>
                    c.GetMethodInfo().Name == nameof(RadarrApi.ICustomFormatApi.CustomformatPost)
                )
                .ElementAt(callIndex)
                .GetArguments()[0]!;
        return sent.Specifications;
    }
}
