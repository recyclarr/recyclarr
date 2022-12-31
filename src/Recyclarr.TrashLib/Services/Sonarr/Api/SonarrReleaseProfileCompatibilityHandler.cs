using AutoMapper;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Recyclarr.TrashLib.Services.Sonarr.Api.Objects;
using Recyclarr.TrashLib.Services.Sonarr.Api.Schemas;
using Serilog;

namespace Recyclarr.TrashLib.Services.Sonarr.Api;

public class SonarrReleaseProfileCompatibilityHandler : ISonarrReleaseProfileCompatibilityHandler
{
    private readonly ILogger _log;
    private readonly Func<SonarrCapabilities> _capabilitiesFactory;
    private readonly IMapper _mapper;

    public SonarrReleaseProfileCompatibilityHandler(
        ILogger log,
        Func<SonarrCapabilities> capabilitiesFactory,
        IMapper mapper)
    {
        _log = log;
        _capabilitiesFactory = capabilitiesFactory;
        _mapper = mapper;
    }

    public object CompatibleReleaseProfileForSending(SonarrReleaseProfile profile)
    {
        var capabilities = _capabilitiesFactory();
        return capabilities.ArraysNeededForReleaseProfileRequiredAndIgnored
            ? profile
            : _mapper.Map<SonarrReleaseProfileV1>(profile);
    }

    public SonarrReleaseProfile CompatibleReleaseProfileForReceiving(JObject profile)
    {
        JSchema? schema;
        IList<string>? errorMessages;

        schema = JSchema.Parse(SonarrReleaseProfileSchema.V2);
        if (profile.IsValid(schema, out errorMessages))
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
