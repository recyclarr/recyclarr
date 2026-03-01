using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide.CustomFormat;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.ServarrApi.CustomFormat;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
internal static partial class SonarrCustomFormatMapper
{
    public static partial CustomFormatResource ToDomain(SonarrApi.CustomFormatResource dto);

    public static partial SonarrApi.CustomFormatResource FromDomain(CustomFormatResource domain);

    private static partial CustomFormatSpecificationData SpecToDomain(
        SonarrApi.CustomFormatSpecificationSchema dto
    );

    private static partial SonarrApi.CustomFormatSpecificationSchema SpecFromDomain(
        CustomFormatSpecificationData domain
    );

    private static partial CustomFormatFieldData FieldToDomain(SonarrApi.Field dto);

    private static partial SonarrApi.Field FieldFromDomain(CustomFormatFieldData domain);
}
