using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Cache;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatTransactionPhase
{
    private readonly ILogger _log;

    public CustomFormatTransactionPhase(ILogger log)
    {
        _log = log;
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public CustomFormatTransactionData Execute(
        IServiceConfiguration config,
        IReadOnlyCollection<CustomFormatData> guideCfs,
        IReadOnlyCollection<CustomFormatData> serviceData,
        CustomFormatCache cache)
    {
        var transactions = new CustomFormatTransactionData();

        foreach (var guideCf in guideCfs)
        {
            _log.Debug("Process transaction for guide CF {TrashId} ({Name})", guideCf.TrashId, guideCf.Name);

            guideCf.Id = cache.FindId(guideCf) ?? 0;

            var serviceCf = FindServiceCfByName(serviceData, guideCf.Name);
            if (serviceCf is not null)
            {
                ProcessExistingCf(config, guideCf, serviceCf, transactions);
                continue;
            }

            serviceCf = FindServiceCfById(serviceData, guideCf.Id);
            if (serviceCf is not null)
            {
                // We do not use AddUpdatedCustomFormat() here because it's impossible for the CFs to be identical if we
                // got to this point. Reason: We reach this code if the names are not the same. At the very least, this
                // means the name needs to be updated in the service.
                transactions.UpdatedCustomFormats.Add(guideCf);
            }
            else
            {
                transactions.NewCustomFormats.Add(guideCf);
            }
        }

        if (config.DeleteOldCustomFormats)
        {
            transactions.DeletedCustomFormats.AddRange(cache.TrashIdMappings
                // Custom format must be in the cache but NOT in the user's config
                .Where(map => guideCfs.All(cf => cf.TrashId != map.TrashId))
                // Also, that cache-only CF must exist in the service (otherwise there is nothing to delete)
                .Where(map => serviceData.Any(cf => cf.Id == map.CustomFormatId)));
        }

        return transactions;
    }

    private void ProcessExistingCf(
        IServiceConfiguration config,
        CustomFormatData guideCf,
        CustomFormatData serviceCf,
        CustomFormatTransactionData transactions)
    {
        if (config.ReplaceExistingCustomFormats)
        {
            // replace:
            // - Use the ID from the service, not the cache, and do an update
            if (guideCf.Id != serviceCf.Id)
            {
                _log.Debug(
                    "Format IDs for CF {Name} did not match which indicates a manually-created CF is " +
                    "replaced, or that the cache is out of sync with the service ({GuideId} != {ServiceId})",
                    serviceCf.Name, guideCf.Id, serviceCf.Id);

                guideCf.Id = serviceCf.Id;
            }

            AddUpdatedCustomFormat(guideCf, serviceCf, transactions);
        }
        else
        {
            // NO replace:
            // - ids must match (can't rename another cf to the same name), otherwise error
            if (guideCf.Id != serviceCf.Id)
            {
                transactions.ConflictingCustomFormats.Add(
                    new ConflictingCustomFormat(guideCf, serviceCf.Id));
            }
            else
            {
                AddUpdatedCustomFormat(guideCf, serviceCf, transactions);
            }
        }
    }

    private static void AddUpdatedCustomFormat(
        CustomFormatData guideCf,
        CustomFormatData serviceCf,
        CustomFormatTransactionData transactions)
    {
        if (!CustomFormatData.Comparer.Equals(guideCf, serviceCf))
        {
            transactions.UpdatedCustomFormats.Add(guideCf);
        }
        else
        {
            transactions.UnchangedCustomFormats.Add(guideCf);
        }
    }

    private static CustomFormatData? FindServiceCfByName(IEnumerable<CustomFormatData> serviceCfs, string cfName)
    {
        return serviceCfs.FirstOrDefault(rcf => cfName.EqualsIgnoreCase(rcf.Name));
    }

    private static CustomFormatData? FindServiceCfById(IEnumerable<CustomFormatData> serviceCfs, int cfId)
    {
        return serviceCfs.FirstOrDefault(rcf => cfId == rcf.Id);
    }
}
