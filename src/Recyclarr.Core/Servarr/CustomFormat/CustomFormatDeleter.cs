using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Servarr.CustomFormat;

internal class CustomFormatDeleter(ILogger log, ICustomFormatService customFormatApi)
    : ICustomFormatDeleter
{
    public async Task<IReadOnlyList<CustomFormatDeleteItem>> GetCandidatesAsync(
        IDeleteCustomFormatSettings settings,
        CancellationToken ct
    )
    {
        var cfs = await customFormatApi.GetCustomFormats(ct);

        if (settings.All)
        {
            return [.. cfs.Select(cf => new CustomFormatDeleteItem(cf.Id, cf.Name))];
        }

        if (settings.CustomFormatNames.Count == 0)
        {
            throw new FatalException(
                "Custom format names must be specified if the `--all` option is not used."
            );
        }

        // Join custom format names from settings against fetched CFs (case-insensitive)
        ILookup<bool, (string Name, IEnumerable<CustomFormatResource> Cfs)> result = settings
            .CustomFormatNames.GroupJoin(
                cfs,
                x => x,
                x => x.Name,
                (x, y) => (Name: x, Cf: y),
                StringComparer.InvariantCultureIgnoreCase
            )
            .ToLookup(x => x.Cf.Any());

        // 'false' means there were no CFs matched to this CF name
        if (result[false].Any())
        {
            var cfNames = result[false].Select(x => x.Name).ToList();
            log.Debug("Unmatched CFs: {Names}", cfNames);
            foreach (var name in cfNames)
            {
                log.Warning("Unmatched CF Name: {Name}", name);
            }
        }

        // 'true' represents CFs that match names provided in user-input
        return
        [
            .. result[true]
                .SelectMany(x => x.Cfs)
                .Select(cf => new CustomFormatDeleteItem(cf.Id, cf.Name)),
        ];
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task<CustomFormatDeleteSummary> DeleteAsync(
        IReadOnlyList<CustomFormatDeleteItem> candidates,
        CancellationToken ct
    )
    {
        ConcurrentBag<string> successNames = [];
        ConcurrentBag<string> failedNames = [];

        var options = new ParallelOptions { MaxDegreeOfParallelism = 8 };
        await Parallel.ForEachAsync(
            candidates,
            options,
            async (candidate, token) =>
            {
                try
                {
                    await customFormatApi.DeleteCustomFormat(candidate.Id, token);
                    successNames.Add(candidate.Name);
                }
                catch (Exception)
                {
                    failedNames.Add(candidate.Name);
                }
            }
        );

        if (!successNames.IsEmpty)
        {
            log.Debug("Deleted custom formats: {@Names}", successNames);
        }

        if (!failedNames.IsEmpty)
        {
            log.Error("Failed to delete custom formats: {@Names}", failedNames);
        }

        return new CustomFormatDeleteSummary(
            successNames.Count,
            failedNames.Count,
            [.. failedNames]
        );
    }
}
