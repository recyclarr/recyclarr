using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.Core.Tests.ServarrApi.CustomFormat;

internal sealed class SonarrV3CustomFormatGatewayTest
{
    [Test, AutoMockData]
    public async Task Create_starts_specification_from_service_schema(
        [Frozen] SonarrApi.ICustomFormatApi api,
        SonarrV3CustomFormatGateway sut
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
        [Frozen] SonarrApi.ICustomFormatApi api,
        SonarrV3CustomFormatGateway sut
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
        [Frozen] SonarrApi.ICustomFormatApi api,
        SonarrV3CustomFormatGateway sut
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
        SonarrApi.ICustomFormatApi api,
        params SonarrApi.CustomFormatSpecificationSchema[] schemas
    )
    {
        api.SpecificationSchema().ReturnsForAnyArgs(schemas);
        api.CustomformatPost(default!)
            .ReturnsForAnyArgs(
                new SonarrApi.CustomFormatResource
                {
                    Id = 1,
                    Name = "CF",
                    IncludeCustomFormatWhenRenaming = false,
                    Specifications = [],
                }
            );
    }

    private static SonarrApi.CustomFormatSpecificationSchema NewReleaseTitleSchema()
    {
        return new SonarrApi.CustomFormatSpecificationSchema
        {
            Implementation = "ReleaseTitleSpecification",
            ImplementationName = "Release Title",
            InfoLink = "https://wiki.example/cf",
            Negate = false,
            Required = false,
            Fields =
            [
                new SonarrApi.Field { Name = "value", Label = "Regular Expression" },
                new SonarrApi.Field { Name = "schemaOnly", Value = "default" },
            ],
            Presets = [new SonarrApi.CustomFormatSpecificationSchema { Name = "preset" }],
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

    private static ICollection<SonarrApi.CustomFormatSpecificationSchema>? SentSpecs(
        SonarrApi.ICustomFormatApi api,
        int callIndex = 0
    )
    {
        var sent = (SonarrApi.CustomFormatResource)
            api.ReceivedCalls()
                .Where(c =>
                    c.GetMethodInfo().Name == nameof(SonarrApi.ICustomFormatApi.CustomformatPost)
                )
                .ElementAt(callIndex)
                .GetArguments()[0]!;
        return sent.Specifications;
    }
}
