using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide.CustomFormat;
using Riok.Mapperly.Abstractions;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.CustomFormat;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class RadarrCustomFormatMapper
{
    public static partial CustomFormatResource ToDomain(RadarrApi.CustomFormatResource dto);

    public static partial RadarrApi.CustomFormatResource FromDomain(CustomFormatResource domain);

    private static partial CustomFormatSpecificationData SpecToDomain(
        RadarrApi.CustomFormatSpecificationSchema dto
    );

    private static partial RadarrApi.CustomFormatSpecificationSchema SpecFromDomain(
        CustomFormatSpecificationData domain
    );

    private static partial CustomFormatFieldData FieldToDomain(RadarrApi.Field dto);

    private static partial RadarrApi.Field FieldFromDomain(CustomFormatFieldData domain);
}
