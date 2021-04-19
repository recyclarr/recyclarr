using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Common
{
    public class ResourceDataReader
    {
        private readonly Assembly? _assembly;
        private readonly string? _namespace;
        private readonly string _subdirectory;

        public ResourceDataReader(Type typeWithNamespaceToUse, string subdirectory = "")
        {
            _subdirectory = subdirectory;
            _namespace = typeWithNamespaceToUse.Namespace;
            _assembly = Assembly.GetAssembly(typeWithNamespaceToUse);
        }

        public string ReadData(string filename)
        {
            var nameBuilder = new StringBuilder();
            nameBuilder.Append(_namespace);
            if (!string.IsNullOrEmpty(_subdirectory))
            {
                nameBuilder.Append($".{_subdirectory}");
            }

            nameBuilder.Append($".{filename}");

            var resourceName = nameBuilder.ToString();
            using var stream = _assembly?.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new ArgumentException($"Embedded resource not found: {resourceName}");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
