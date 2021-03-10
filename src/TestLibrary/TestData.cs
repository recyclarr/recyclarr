using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace TestLibrary
{
    public class TestData<TTestFixtureClass>
    {
        private readonly Assembly? _assembly;
        private readonly string? _namespace;

        public TestData()
        {
            var attributes = typeof(TTestFixtureClass).GetCustomAttributes(typeof(TestFixtureAttribute), true);
            if (attributes.Length == 0)
            {
                throw new ArgumentException(
                    $"{typeof(TTestFixtureClass).Name} does not have the [TestFixture] attribute");
            }

            _namespace = typeof(TTestFixtureClass).Namespace;
            _assembly = Assembly.GetAssembly(typeof(TTestFixtureClass));
        }

        public string DataSubdirectoryName { get; set; } = "Data";

        public string GetResourceData(string name)
        {
            var resourceName = $"{_namespace}.{DataSubdirectoryName}.{name}";
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
