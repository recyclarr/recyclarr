using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatTransactionPhase(ILogger log, IServiceConfiguration config)
    : IPipelinePhase<CustomFormatPipelineContext>
{
    public Task<PipelineFlow> Execute(CustomFormatPipelineContext context, CancellationToken ct)
    {
        var transactions = new CustomFormatTransactionData();

        foreach (var guideCf in context.ConfigOutput)
        {
            log.Debug(
                "Process transaction for guide CF {TrashId} ({Name})",
                guideCf.TrashId,
                guideCf.Name
            );

            guideCf.Id = context.Cache.FindId(guideCf) ?? 0;

            var serviceCf = FindServiceCfByName(context.ApiFetchOutput, guideCf.Name);
            if (serviceCf is not null)
            {
                ProcessExistingCf(guideCf, serviceCf, transactions);
                continue;
            }

            serviceCf = FindServiceCfById(context.ApiFetchOutput, guideCf.Id);
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
            transactions.DeletedCustomFormats.AddRange(
                context
                    .Cache.Mappings
                    // Custom format must be in the cache but NOT in the user's config
                    .Where(map => context.ConfigOutput.All(cf => cf.TrashId != map.TrashId))
                    // Also, that cache-only CF must exist in the service (otherwise there is nothing to delete)
                    .Where(map => context.ApiFetchOutput.Any(cf => cf.Id == map.ServiceId))
            );
        }

        context.TransactionOutput = transactions;
        return Task.FromResult(PipelineFlow.Continue);
    }

    private void ProcessExistingCf(
        CustomFormatResource guideCf,
        CustomFormatResource serviceCf,
        CustomFormatTransactionData transactions
    )
    {
        if (config.ReplaceExistingCustomFormats)
        {
            // replace:
            // - Use the ID from the service, not the cache, and do an update
            if (guideCf.Id != serviceCf.Id)
            {
                log.Debug(
                    "Format IDs for CF {Name} did not match which indicates a manually-created CF is "
                        + "replaced, or that the cache is out of sync with the service ({GuideId} != {ServiceId})",
                    serviceCf.Name,
                    guideCf.Id,
                    serviceCf.Id
                );

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
                    new ConflictingCustomFormat(guideCf, serviceCf.Id)
                );
            }
            else
            {
                AddUpdatedCustomFormat(guideCf, serviceCf, transactions);
            }
        }
    }

    private static void AddUpdatedCustomFormat(
        CustomFormatResource guideCf,
        CustomFormatResource serviceCf,
        CustomFormatTransactionData transactions
    )
    {
        // Use IsEquivalentTo() instead of Equals() or == operator.
        // Guide CFs are SonarrCustomFormatResource/RadarrCustomFormatResource (derived types),
        // while API responses deserialize to base CustomFormatResource. C# record inheritance
        // causes Equals() to fail across type boundaries due to EqualityContract checks.
        // IsEquivalentTo() is a non-virtual method that compares only the relevant properties.
        if (!guideCf.IsEquivalentTo(serviceCf))
        {
            transactions.UpdatedCustomFormats.Add(guideCf);
        }
        else
        {
            transactions.UnchangedCustomFormats.Add(guideCf);
        }
    }

    private static CustomFormatResource? FindServiceCfByName(
        IEnumerable<CustomFormatResource> serviceCfs,
        string cfName
    )
    {
        return serviceCfs.FirstOrDefault(rcf => cfName.EqualsIgnoreCase(rcf.Name));
    }

    private static CustomFormatResource? FindServiceCfById(
        IEnumerable<CustomFormatResource> serviceCfs,
        int cfId
    )
    {
        return serviceCfs.FirstOrDefault(rcf => cfId == rcf.Id);
    }
}
