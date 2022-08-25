using Common.FluentValidation;
using FluentValidation.Results;
using Serilog;

namespace TrashLib.Services.Sonarr.ReleaseProfile.Filters;

public class ReleaseProfileDataValidationFilterer
{
    private readonly ILogger _log;

    public ReleaseProfileDataValidationFilterer(ILogger log)
    {
        _log = log;
    }

    private void LogInvalidTerm(List<ValidationFailure> failures, string filterDescription)
    {
        _log.Debug("Validation failed on term data ({Filter}): {Failures}", filterDescription, failures);
    }

    public IEnumerable<TermData> FilterTerms(IEnumerable<TermData> terms)
    {
        return terms.IsValid(new TermDataValidator(), (e, x) => LogInvalidTerm(e, x.ToString()));
    }

    public IEnumerable<PreferredTermData> FilterTerms(IEnumerable<PreferredTermData> terms)
    {
        return terms.IsValid(new PreferredTermDataValidator(), (e, x) => LogInvalidTerm(e, x.ToString()));
    }

    private ReleaseProfileData FilterProfile(ReleaseProfileData profile)
    {
        return profile with
        {
            Required = FilterTerms(profile.Required).ToList(),
            Ignored = FilterTerms(profile.Ignored).ToList(),
            Preferred = FilterTerms(profile.Preferred).ToList()
        };
    }

    public IEnumerable<ReleaseProfileData> FilterProfiles(IEnumerable<ReleaseProfileData> data)
    {
        return data
            .Select(FilterProfile)
            .IsValid(new ReleaseProfileDataValidator(), (e, x) =>
            {
                _log.Warning("Excluding invalid release profile: {Profile}", x.ToString());
                _log.Debug("Release profile excluded for these reasons: {Reasons}", e);
            });
    }
}
