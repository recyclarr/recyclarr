using AutoMapper;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Api.Objects;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Api.Schemas;
using Recyclarr.TrashLib.Compatibility.Sonarr;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Api;

public class SonarrReleaseProfileCompatibilityHandler : ISonarrReleaseProfileCompatibilityHandler
{
    private readonly ILogger _log;
    private readonly ISonarrCapabilityFetcher _capabilityFetcher;
    private readonly IMapper _mapper;

    public SonarrReleaseProfileCompatibilityHandler(
        ILogger log,
        ISonarrCapabilityFetcher capabilityFetcher,
        IMapper mapper)
    {
        _log = log;
        _capabilityFetcher = capabilityFetcher;
        _mapper = mapper;
    }

    public async Task<object> CompatibleReleaseProfileForSending(
        IServiceConfiguration config,
        SonarrReleaseProfile profile)
    {
        var capabilities = await _capabilityFetcher.GetCapabilities(config);

        return capabilities.ArraysNeededForReleaseProfileRequiredAndIgnored
            ? profile
            : _mapper.Map<SonarrReleaseProfileV1>(profile);
    }

    public SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile)
    {
        var schema = JSchema.Parse(SonarrReleaseProfileSchema.V2);
        if (profile.IsValid(schema, out IList<string>? errorMessages))
        {
            return profile.ToObject<SonarrReleaseProfile>()
                ?? throw new InvalidDataException("SonarrReleaseProfile V2 parsing failed");
        }

        _log.Debug("SonarrReleaseProfile is not a match for V2, proceeding to V1: {Reasons}", errorMessages);

        schema = JSchema.Parse(SonarrReleaseProfileSchema.V1);
        if (profile.IsValid(schema, out errorMessages))
        {
            // This will throw if there's an issue during mapping.
            return _mapper.Map<SonarrReleaseProfile>(profile.ToObject<SonarrReleaseProfileV1>());
        }

        throw new InvalidDataException(
            $"SonarrReleaseProfile expected, but no supported schema detected: {errorMessages}");
    }
}
