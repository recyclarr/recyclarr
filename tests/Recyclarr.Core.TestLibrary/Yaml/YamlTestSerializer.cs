using System.IO.Abstractions.TestingHelpers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Recyclarr.Core.TestLibrary.Yaml;

public static class YamlTestSerializer
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public static string ToYaml<T>(T obj) => Serializer.Serialize(obj);

    public static MockFileData ToMockYaml<T>(T obj) => new(ToYaml(obj));
}
