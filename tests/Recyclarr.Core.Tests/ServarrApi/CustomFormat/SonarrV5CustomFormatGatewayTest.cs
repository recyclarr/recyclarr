using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;
using SonarrApi = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.Core.Tests.ServarrApi.CustomFormat;

internal sealed class SonarrV5CustomFormatGatewayTest
{
    [Test, AutoMockData]
    public async Task Fetch_maps_custom_formats_to_domain(
        [Frozen] SonarrApi.ICustomFormatApi api,
        SonarrV5CustomFormatGateway sut
    )
    {
        api.CustomformatGet()
            .ReturnsForAnyArgs([
                new SonarrApi.CustomFormatResource
                {
                    Id = 7,
                    Name = "CF",
                    IncludeCustomFormatWhenRenaming = true,
                    Specifications =
                    [
                        new SonarrApi.CustomFormatSpecificationSchema
                        {
                            Name = "x",
                            Implementation = "ReleaseTitleSpecification",
                            Negate = false,
                            Required = true,
                            Fields = [],
                        },
                    ],
                },
            ]);

        var result = await sut.GetCustomFormats(CancellationToken.None);

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

    [Test, AutoMockData]
    public async Task Update_sends_custom_format_to_its_id_with_specification_metadata(
        [Frozen] SonarrApi.ICustomFormatApi api,
        SonarrV5CustomFormatGateway sut
    )
    {
        api.Schema()
            .ReturnsForAnyArgs([
                new SonarrApi.CustomFormatSpecificationSchema
                {
                    Implementation = "ReleaseTitleSpecification",
                    ImplementationName = "Release Title",
                    InfoLink = "https://wiki.example/cf",
                },
            ]);

        await sut.UpdateCustomFormat(
            new CustomFormatResource
            {
                Id = 7,
                Name = "CF",
                Specifications =
                [
                    new CustomFormatSpecificationData
                    {
                        Name = "x",
                        Implementation = "ReleaseTitleSpecification",
                    },
                ],
            },
            CancellationToken.None
        );

        var call = api.ReceivedCalls()
            .Single(c =>
                c.GetMethodInfo().Name == nameof(SonarrApi.ICustomFormatApi.CustomformatPut)
            );
        call.GetArguments()[0].Should().Be("7");
        ((SonarrApi.CustomFormatResource)call.GetArguments()[1]!)
            .Specifications.Should()
            .ContainSingle()
            .Which.ImplementationName.Should()
            .Be("Release Title");
    }

    [Test, AutoMockData]
    public async Task Create_starts_specification_from_service_schema(
        [Frozen] SonarrApi.ICustomFormatApi api,
        SonarrV5CustomFormatGateway sut
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
        SonarrV5CustomFormatGateway sut
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
        SonarrV5CustomFormatGateway sut
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
        api.Schema().ReturnsForAnyArgs(schemas);
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
