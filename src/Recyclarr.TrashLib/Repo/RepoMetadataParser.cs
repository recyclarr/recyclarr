using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Repo;

public class RepoMetadataParser : IRepoMetadataParser
{
    private readonly IAppPaths _paths;

    public RepoMetadataParser(IAppPaths paths)
    {
        _paths = paths;
    }

    public RepoMetadata Deserialize()
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        });

        var metadataFile = _paths.RepoDirectory.File("metadata.json");
        using var stream = new JsonTextReader(metadataFile.OpenText());

        var metadata = serializer.Deserialize<RepoMetadata>(stream);
        if (metadata is null)
        {
            throw new InvalidDataException("Unable to deserialize metadata.json");
        }

        return metadata;
    }
}
