using System.Runtime.Serialization;
using Recyclarr.Config.Parsing.ErrorHandling;
using YamlDotNet.Serialization;

namespace Recyclarr.Yaml.YamlDotNet;

internal sealed class DeprecatedPropertyInspector(
    ITypeInspector inner,
    IConfigDiagnosticCollector collector
) : ITypeInspector
{
    // Known removed properties that should produce a warning instead of a parse error.
    // The key is the YAML property name (after naming convention is applied).
    private static readonly Dictionary<string, string> DeprecatedProperties = new(
        StringComparer.Ordinal
    )
    {
        ["replace_existing_custom_formats"] =
            "The `replace_existing_custom_formats` option has been removed and will be ignored. "
            + "See: https://recyclarr.dev/guide/upgrade-guide/v8.0/#replace-existing-removed",
    };

    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        return inner.GetProperties(type, container);
    }

    public IPropertyDescriptor GetProperty(
        Type type,
        object? container,
        string name,
        bool ignoreUnmatched,
        bool caseInsensitivePropertyMatching
    )
    {
        try
        {
            return inner.GetProperty(
                type,
                container,
                name,
                ignoreUnmatched,
                caseInsensitivePropertyMatching
            );
        }
        catch (SerializationException)
        {
            // Property not found on the type. Check if it's a known deprecated property.
            if (DeprecatedProperties.TryGetValue(name, out var message))
            {
                collector.AddDeprecation(message);

                // Return null to skip this property (YamlDotNet advances past the value)
                return null!;
            }

            throw;
        }
    }

    public string GetEnumName(Type enumType, string name)
    {
        return inner.GetEnumName(enumType, name);
    }

    public string GetEnumValue(object enumValue)
    {
        return inner.GetEnumValue(enumValue);
    }
}
