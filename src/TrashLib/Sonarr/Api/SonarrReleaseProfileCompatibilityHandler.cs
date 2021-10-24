using System.Collections.Generic;
using System.IO;
using AutoMapper;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Serilog;
using TrashLib.Sonarr.Api.Objects;
using TrashLib.Sonarr.Api.Schemas;

namespace TrashLib.Sonarr.Api
{
    public class SonarrReleaseProfileCompatibilityHandler : ISonarrReleaseProfileCompatibilityHandler
    {
        private readonly ISonarrCompatibility _compatibility;
        private readonly IMapper _mapper;

        public SonarrReleaseProfileCompatibilityHandler(
            ISonarrCompatibility compatibility,
            IMapper mapper)
        {
            _compatibility = compatibility;
            _mapper = mapper;
        }

        public object CompatibleReleaseProfileForSending(SonarrReleaseProfile profile)
        {
            return _compatibility.ArraysNeededForReleaseProfileRequiredAndIgnored
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

            Log.Debug("SonarrReleaseProfile is not a match for V2, proceeding to V1: {Reasons}", errorMessages);

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
}