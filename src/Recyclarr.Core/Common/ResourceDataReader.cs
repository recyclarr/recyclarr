using System.Globalization;
using System.Reflection;
using System.Text;

namespace Recyclarr.Common;

public class ResourceDataReader(Assembly assembly, string subdirectory = "") : IResourceDataReader
{
    private readonly string? _namespace;

    public ResourceDataReader(Type typeWithNamespaceToUse, string subdirectory = "")
        : this(
            Assembly.GetAssembly(typeWithNamespaceToUse)
                ?? throw new ArgumentException(
                    "Cannot get assembly from type",
                    nameof(typeWithNamespaceToUse)
                ),
            subdirectory
        )
    {
        _namespace = typeWithNamespaceToUse.Namespace;
    }

    public string ReadData(string filename)
    {
        var resourcePath = BuildResourceName(filename);
        var foundResource = FindResourcePath(resourcePath);
        return GetResourceData(foundResource);
    }

    private string BuildResourceName(string filename)
    {
        var nameBuilder = new StringBuilder();

        if (!string.IsNullOrEmpty(_namespace))
        {
            nameBuilder.Append(CultureInfo.InvariantCulture, $"{_namespace}.");
        }

        if (!string.IsNullOrEmpty(subdirectory))
        {
            nameBuilder.Append(CultureInfo.InvariantCulture, $"{subdirectory}.");
        }

        nameBuilder.Append(filename);
        return nameBuilder.ToString();
    }

    private string FindResourcePath(string resourcePath)
    {
        var foundResource = Array.Find(
            assembly.GetManifestResourceNames(),
            x => x.EndsWith(resourcePath, StringComparison.Ordinal)
        );

        return foundResource
            ?? throw new ArgumentException($"Embedded resource not found: {resourcePath}");
    }

    private string GetResourceData(string resourcePath)
    {
        using var stream =
            assembly.GetManifestResourceStream(resourcePath)
            ?? throw new ArgumentException($"Unable to open embedded resource: {resourcePath}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
