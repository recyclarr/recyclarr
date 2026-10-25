using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide.CustomFormat;
using Riok.Mapperly.Abstractions;
using SonarrApi = Recyclarr.Api.Sonarr.V3;

namespace Recyclarr.ServarrApi.CustomFormat;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, UseDeepCloning = true)]
internal static partial class SonarrV3CustomFormatMapper
{
    public static partial CustomFormatResource ToDomain(SonarrApi.CustomFormatResource dto);

    public static partial SonarrApi.CustomFormatResource FromDomain(CustomFormatResource domain);

    private static partial CustomFormatSpecificationData SpecToDomain(
        SonarrApi.CustomFormatSpecificationSchema dto
    );

    public static partial SonarrApi.CustomFormatSpecificationSchema SpecFromDomain(
        CustomFormatSpecificationData domain
    );

    // Presets are UI starting points for a new specification, not part of a saved one
    [MapperIgnoreTarget(nameof(SonarrApi.CustomFormatSpecificationSchema.Presets))]
    public static partial SonarrApi.CustomFormatSpecificationSchema CloneSpec(
        SonarrApi.CustomFormatSpecificationSchema schema
    );

    [MapperIgnoreSource(nameof(CustomFormatSpecificationData.Fields))]
    public static partial void UpdateSpec(
        CustomFormatSpecificationData domain,
        [MappingTarget] SonarrApi.CustomFormatSpecificationSchema dto
    );

    private static partial CustomFormatFieldData FieldToDomain(SonarrApi.Field dto);

    public static partial SonarrApi.Field FieldFromDomain(CustomFormatFieldData domain);

    public static partial void UpdateField(
        CustomFormatFieldData domain,
        [MappingTarget] SonarrApi.Field dto
    );

    // Normalizes object? (JsonElement from Refit) to FieldValue with proper equality semantics
    private static FieldValue MapFieldValue(object? value) => FieldValue.From(value);

    private static object? MapFieldValueToObject(FieldValue value) => value.Inner;
}
