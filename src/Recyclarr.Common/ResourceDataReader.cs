using System.Reflection;
using System.Text;

namespace Recyclarr.Common;

public class ResourceDataReader : IResourceDataReader
{
    private readonly Assembly _assembly;
    private readonly string? _namespace;
    private readonly string _subdirectory;

    public ResourceDataReader(Assembly assembly, string subdirectory = "")
    {
        _subdirectory = subdirectory;
        _assembly = assembly;
    }

    public ResourceDataReader(Type typeWithNamespaceToUse, string subdirectory = "")
    {
        _subdirectory = subdirectory;
        _namespace = typeWithNamespaceToUse.Namespace;
        _assembly = Assembly.GetAssembly(typeWithNamespaceToUse)
            ?? throw new ArgumentException("Cannot get assembly from type", nameof(typeWithNamespaceToUse));
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
            nameBuilder.Append($"{_namespace}.");
        }

        if (!string.IsNullOrEmpty(_subdirectory))
        {
            nameBuilder.Append($"{_subdirectory}.");
        }

        nameBuilder.Append(filename);
        return nameBuilder.ToString();
    }

    private string FindResourcePath(string resourcePath)
    {
        var foundResource = Array.Find(_assembly.GetManifestResourceNames(), x => x.EndsWith(resourcePath));
        if (foundResource is null)
        {
            throw new ArgumentException($"Embedded resource not found: {resourcePath}");
        }

        return foundResource;
    }

    private string GetResourceData(string resourcePath)
    {
        using var stream = _assembly.GetManifestResourceStream(resourcePath);
        if (stream is null)
        {
            throw new ArgumentException($"Unable to open embedded resource: {resourcePath}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
