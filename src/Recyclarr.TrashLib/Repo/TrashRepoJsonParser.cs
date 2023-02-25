using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Recyclarr.TrashLib.Repo;

public static class TrashRepoJsonParser
{
    public static T Deserialize<T>(IFileInfo jsonFile)
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        });

        using var stream = new JsonTextReader(jsonFile.OpenText());

        var obj = serializer.Deserialize<T>(stream);
        if (obj is null)
        {
            throw new InvalidDataException($"Unable to deserialize {jsonFile}");
        }

        return obj;
    }
}
