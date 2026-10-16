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
    public async Task Create_fills_specification_metadata_required_by_v5(
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

        await sut.CreateCustomFormat(
            new CustomFormatResource
            {
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

        var sent = (SonarrApi.CustomFormatResource)
            api.ReceivedCalls()
                .Single(c =>
                    c.GetMethodInfo().Name == nameof(SonarrApi.ICustomFormatApi.CustomformatPost)
                )
                .GetArguments()[0]!;
        sent.Specifications.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new { ImplementationName = "Release Title", InfoLink = "https://wiki.example/cf" }
            );
    }
}
