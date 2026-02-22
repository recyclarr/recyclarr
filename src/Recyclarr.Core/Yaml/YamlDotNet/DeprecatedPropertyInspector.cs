using System.Runtime.Serialization;
using Recyclarr.Config.Parsing.ErrorHandling;
using YamlDotNet.Serialization;

namespace Recyclarr.Yaml.YamlDotNet;

internal enum RemovedPropertySeverity
{
    Warning,
    Error,
}

internal record RemovedPropertyEntry(string Message, RemovedPropertySeverity Severity);

/// <summary>
/// Intercepts property lookup during YAML deserialization. When a property isn't found on the
/// target type, checks if it's a known removed property. Warning-severity entries are skipped with
/// a deprecation report so parsing continues. Error-severity entries throw a
/// ConfigParsingException to block sync with a helpful message.
/// </summary>
internal sealed class DeprecatedPropertyInspector(
    ITypeInspector inner,
    IReadOnlyDictionary<string, RemovedPropertyEntry> removedProperties,
    Action<string> reportDeprecation
) : ITypeInspector
{
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
            if (!removedProperties.TryGetValue(name, out var entry))
            {
                throw;
            }

            if (entry.Severity is RemovedPropertySeverity.Error)
            {
                throw new ConfigParsingException(entry.Message, 0, new SerializationException());
            }

            reportDeprecation(entry.Message);
            return null!;
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
