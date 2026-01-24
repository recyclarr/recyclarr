using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;

namespace Recyclarr.EndToEndTests.Clients;

internal sealed class ServarrTestClient(string baseUrl, string apiKey)
{
    public async Task<List<FauxCustomFormat>> GetCustomFormats(CancellationToken ct = default)
    {
        return await baseUrl
            .AppendPathSegment("api/v3/customformat")
            .WithHeader("X-Api-Key", apiKey)
            .GetJsonAsync<List<FauxCustomFormat>>(cancellationToken: ct);
    }

    public async Task RenameCustomFormat(int id, string newName, CancellationToken ct = default)
    {
        var cf = await baseUrl
            .AppendPathSegment($"api/v3/customformat/{id}")
            .WithHeader("X-Api-Key", apiKey)
            .GetJsonAsync<FauxCustomFormat>(cancellationToken: ct);

        cf.Name = newName;

        await baseUrl
            .AppendPathSegment($"api/v3/customformat/{id}")
            .WithHeader("X-Api-Key", apiKey)
            .PutJsonAsync(cf, cancellationToken: ct);
    }

    public async Task DeleteCustomFormat(int id, CancellationToken ct = default)
    {
        await baseUrl
            .AppendPathSegment($"api/v3/customformat/{id}")
            .WithHeader("X-Api-Key", apiKey)
            .DeleteAsync(cancellationToken: ct);
    }

    public async Task<List<FauxQualityProfile>> GetQualityProfiles(CancellationToken ct = default)
    {
        return await baseUrl
            .AppendPathSegment("api/v3/qualityprofile")
            .WithHeader("X-Api-Key", apiKey)
            .GetJsonAsync<List<FauxQualityProfile>>(cancellationToken: ct);
    }

    public async Task<List<FauxQualityDefinition>> GetQualityDefinitions(
        CancellationToken ct = default
    )
    {
        return await baseUrl
            .AppendPathSegment("api/v3/qualitydefinition")
            .WithHeader("X-Api-Key", apiKey)
            .GetJsonAsync<List<FauxQualityDefinition>>(cancellationToken: ct);
    }

    public async Task<FauxMediaManagement> GetMediaManagement(CancellationToken ct = default)
    {
        return await baseUrl
            .AppendPathSegment("api/v3/config/mediamanagement")
            .WithHeader("X-Api-Key", apiKey)
            .GetJsonAsync<FauxMediaManagement>(cancellationToken: ct);
    }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
internal sealed class FauxCustomFormat
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraProperties { get; set; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
internal sealed record FauxQualityProfile
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int MinFormatScore { get; init; }
    public int MinUpgradeFormatScore { get; init; }
    public bool UpgradeAllowed { get; init; }
    public FauxProfileLanguage? Language { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
internal sealed record FauxProfileLanguage
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
internal sealed record FauxQualityDefinition
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public decimal MinSize { get; init; }
    public decimal? MaxSize { get; init; }
    public decimal? PreferredSize { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
internal sealed record FauxMediaManagement
{
    public int Id { get; init; }
    public string DownloadPropersAndRepacks { get; init; } = "";
}
