using System.Collections.ObjectModel;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;

public class ReleaseProfileDataFilterer
{
    private readonly ILogger _log;
    private readonly ReleaseProfileDataValidationFilterer _validator;

    public ReleaseProfileDataFilterer(ILogger log)
    {
        _log = log;
        _validator = new ReleaseProfileDataValidationFilterer(log);
    }

    public ReadOnlyCollection<TermData> ExcludeTerms(
        IEnumerable<TermData> terms,
        IEnumerable<string> excludeFilter)
    {
        var result = terms.Where(x => !excludeFilter.Contains(x.TrashId, StringComparer.InvariantCultureIgnoreCase));
        return _validator.FilterTerms(result).ToList().AsReadOnly();
    }

    public ReadOnlyCollection<PreferredTermData> ExcludeTerms(
        IEnumerable<PreferredTermData> terms,
        IReadOnlyCollection<string> excludeFilter)
    {
        var result = terms
            .Select(x => new PreferredTermData
            {
                Score = x.Score,
                Terms = ExcludeTerms(x.Terms, excludeFilter)
            });

        return _validator.FilterTerms(result).ToList().AsReadOnly();
    }

    public ReadOnlyCollection<TermData> IncludeTerms(
        IEnumerable<TermData> terms,
        IEnumerable<string> includeFilter)
    {
        var result = terms.Where(x => includeFilter.Contains(x.TrashId, StringComparer.InvariantCultureIgnoreCase));
        return _validator.FilterTerms(result).ToList().AsReadOnly();
    }

    public ReadOnlyCollection<PreferredTermData> IncludeTerms(
        IEnumerable<PreferredTermData> terms,
        IReadOnlyCollection<string> includeFilter)
    {
        var result = terms
            .Select(x => new PreferredTermData
            {
                Score = x.Score,
                Terms = IncludeTerms(x.Terms, includeFilter)
            });

        return _validator.FilterTerms(result).ToList().AsReadOnly();
    }

    public ReleaseProfileData? FilterProfile(
        ReleaseProfileData selectedProfile,
        SonarrProfileFilterConfig profileFilter)
    {
        if (profileFilter.Include.Any())
        {
            _log.Debug("Using inclusion filter");
            return selectedProfile with
            {
                Required = IncludeTerms(selectedProfile.Required, profileFilter.Include),
                Ignored = IncludeTerms(selectedProfile.Ignored, profileFilter.Include),
                Preferred = IncludeTerms(selectedProfile.Preferred, profileFilter.Include)
            };
        }

        if (profileFilter.Exclude.Any())
        {
            _log.Debug("Using exclusion filter");
            return selectedProfile with
            {
                Required = ExcludeTerms(selectedProfile.Required, profileFilter.Exclude),
                Ignored = ExcludeTerms(selectedProfile.Ignored, profileFilter.Exclude),
                Preferred = ExcludeTerms(selectedProfile.Preferred, profileFilter.Exclude)
            };
        }

        _log.Debug("Filter property present but is empty");
        return null;
    }
}
