using System.Collections.Generic;
using System.Threading.Tasks;
using TrashLib.Radarr.CustomFormat.Api.Models;

namespace TrashLib.Radarr.CustomFormat.Api
{
    public interface IQualityProfileService
    {
        Task<List<QualityProfileData>> GetQualityProfiles();
        Task<QualityProfileData> UpdateQualityProfile(QualityProfileData profile, int profileId);
    }
}
