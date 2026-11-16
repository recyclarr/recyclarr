using System.Text.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.TrashGuide.CustomFormat;
using Riok.Mapperly.Abstractions;
using RadarrApi = Recyclarr.Api.Radarr;

namespace Recyclarr.ServarrApi.CustomFormat;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, UseDeepCloning = true)]
internal static partial class RadarrCustomFormatMapper
{
    public static partial CustomFormatResource ToDomain(RadarrApi.CustomFormatResource dto);

    public static partial RadarrApi.CustomFormatResource FromDomain(CustomFormatResource domain);

    private static partial CustomFormatSpecificationData SpecToDomain(
        RadarrApi.CustomFormatSpecificationSchema dto
    );

    public static partial RadarrApi.CustomFormatSpecificationSchema SpecFromDomain(
        CustomFormatSpecificationData domain
    );

    public static partial RadarrApi.CustomFormatSpecificationSchema CloneSpec(
        RadarrApi.CustomFormatSpecificationSchema schema
    );

    [MapperIgnoreSource(nameof(CustomFormatSpecificationData.Fields))]
    public static partial void UpdateSpec(
        CustomFormatSpecificationData domain,
        [MappingTarget] RadarrApi.CustomFormatSpecificationSchema dto
    );

    private static partial CustomFormatFieldData FieldToDomain(RadarrApi.Field dto);

    public static partial RadarrApi.Field FieldFromDomain(CustomFormatFieldData domain);

    public static partial void UpdateField(
        CustomFormatFieldData domain,
        [MappingTarget] RadarrApi.Field dto
    );

    // Normalizes object? (JsonElement from Refit) to FieldValue with proper equality semantics
    private static FieldValue MapFieldValue(object? value) => FieldValue.From(value);

    private static object? MapFieldValueToObject(FieldValue value) => value.Inner;

    // Deep cloning of unmodeled extension data; JsonElement has no members Mapperly can copy
    private static JsonElement CloneElement(JsonElement element) => element.Clone();
}
