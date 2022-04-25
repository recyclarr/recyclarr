using System.Collections.ObjectModel;
using Common.FluentValidation;
using FluentValidation.Results;
using Serilog;
using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile;

public class ReleaseProfileDataFilterer
{
    private readonly ILogger _log;

    public ReleaseProfileDataFilterer(ILogger log)
    {
        _log = log;
    }

    private void LogInvalidTerm(List<ValidationFailure> failures, string filterDescription)
    {
        _log.Debug("Validation failed on term data ({Filter}): {Failures}", filterDescription, failures);
    }

    public ReadOnlyCollection<TermData> ExcludeTerms(IEnumerable<TermData> terms,
        IEnumerable<string> includeFilter)
    {
        return terms
            .ExceptBy(includeFilter, x => x.TrashId, StringComparer.InvariantCultureIgnoreCase)
            .IsValid(new TermDataValidator(), (e, x) => LogInvalidTerm(e, $"Exclude: {x}"))
            .ToList().AsReadOnly();
    }

    public ReadOnlyCollection<PreferredTermData> ExcludeTerms(IEnumerable<PreferredTermData> terms,
        IReadOnlyCollection<string> includeFilter)
    {
        return terms
            .Select(x => new PreferredTermData
            {
                Score = x.Score,
                Terms = ExcludeTerms(x.Terms, includeFilter)
            })
            .IsValid(new PreferredTermDataValidator(), (e, x) => LogInvalidTerm(e, $"Exclude Preferred: {x}"))
            .ToList()
            .AsReadOnly();
    }

    public ReadOnlyCollection<TermData> IncludeTerms(IEnumerable<TermData> terms,
        IEnumerable<string> includeFilter)
    {
        return terms
            .IntersectBy(includeFilter, x => x.TrashId, StringComparer.InvariantCultureIgnoreCase)
            .IsValid(new TermDataValidator(),
                (e, x) => LogInvalidTerm(e, $"Include: {x}"))
            .ToList().AsReadOnly();
    }

    public ReadOnlyCollection<PreferredTermData> IncludeTerms(IEnumerable<PreferredTermData> terms,
        IReadOnlyCollection<string> includeFilter)
    {
        return terms
            .Select(x => new PreferredTermData
            {
                Score = x.Score,
                Terms = IncludeTerms(x.Terms, includeFilter)
            })
            .IsValid(new PreferredTermDataValidator(), (e, x) => LogInvalidTerm(e, $"Include Preferred {x}"))
            .ToList()
            .AsReadOnly();
    }

    public ReleaseProfileData? FilterProfile(ReleaseProfileData selectedProfile,
        SonarrProfileFilterConfig profileFilter)
    {
        if (profileFilter.Include.Any())
        {
            _log.Debug("Using inclusion filter");
            return new ReleaseProfileData
            {
                TrashId = selectedProfile.TrashId,
                Name = selectedProfile.Name,
                IncludePreferredWhenRenaming = selectedProfile.IncludePreferredWhenRenaming,
                Required = IncludeTerms(selectedProfile.Required, profileFilter.Include),
                Ignored = IncludeTerms(selectedProfile.Ignored, profileFilter.Include),
                Preferred = IncludeTerms(selectedProfile.Preferred, profileFilter.Include)
            };
        }

        if (profileFilter.Exclude.Any())
        {
            _log.Debug("Using exclusion filter");
            return new ReleaseProfileData
            {
                TrashId = selectedProfile.TrashId,
                Name = selectedProfile.Name,
                IncludePreferredWhenRenaming = selectedProfile.IncludePreferredWhenRenaming,
                Required = ExcludeTerms(selectedProfile.Required, profileFilter.Exclude),
                Ignored = ExcludeTerms(selectedProfile.Ignored, profileFilter.Exclude),
                Preferred = ExcludeTerms(selectedProfile.Preferred, profileFilter.Exclude)
            };
        }

        _log.Debug("Filter property present but is empty");
        return null;
    }
}
