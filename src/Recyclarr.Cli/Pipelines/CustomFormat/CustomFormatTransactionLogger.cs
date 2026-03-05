using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class CustomFormatTransactionLogger(ILogger log)
{
    public void LogTransactions(CustomFormatPipelineContext context)
    {
        var transactions = context.TransactionOutput;

        var hasBlockingErrors = LogDiagnostics(context.Publisher, transactions);
        if (hasBlockingErrors)
        {
            throw new PipelineInterruptException();
        }

        var totalCount = LogResults(transactions);

        context.Publisher.SetStatus(PipelineProgressStatus.Succeeded, totalCount);
    }

    private int LogResults(CustomFormatTransactionData transactions)
    {
        var created = transactions.NewCustomFormats;
        if (created.Count > 0)
        {
            log.Information("Created {Count} New Custom Formats", created.Count);

            foreach (var cf in created)
            {
                log.Debug("> Created: {TrashId} ({Name})", cf.TrashId, cf.Name);
            }
        }

        var updated = transactions.UpdatedCustomFormats;
        if (updated.Count > 0)
        {
            log.Information("Updated {Count} Existing Custom Formats", updated.Count);

            foreach (var cf in updated)
            {
                log.Debug("> Updated: {TrashId} ({Name})", cf.TrashId, cf.Name);
            }
        }

        var skipped = transactions.UnchangedCustomFormats;
        if (skipped.Count > 0)
        {
            log.Information("Skipped {Count} Custom Formats that did not change", skipped.Count);
            log.Debug(
                "Custom Formats Skipped: {CustomFormats}",
                skipped.ToDictionary(k => k.TrashId, v => v.Name)
            );

            // Do not print skipped CFs to console; they are too verbose
        }

        var deleted = transactions.DeletedCustomFormats;
        if (deleted.Count > 0)
        {
            log.Information("Deleted {Count} Custom Formats", deleted.Count);

            foreach (var mapping in deleted)
            {
                log.Debug("> Deleted: {TrashId} ({Name})", mapping.TrashId, mapping.Name);
            }
        }

        var totalCount = transactions.TotalCustomFormatChanges;
        if (totalCount > 0)
        {
            log.Information("Total of {Count} custom formats were synced", totalCount);
        }
        else
        {
            log.Information("All custom formats are already up to date!");
        }

        return totalCount;
    }

    private static bool LogDiagnostics(
        IPipelinePublisher publisher,
        CustomFormatTransactionData transactions
    )
    {
        LogReplacedCustomFormats(publisher, transactions);

        foreach (var ambiguous in transactions.AmbiguousCustomFormats)
        {
            var matchList = string.Join(
                ", ",
                ambiguous.ServiceMatches.Select(m => $"\"{m.Name}\" (ID: {m.Id})")
            );
            var message =
                $"Custom Format '{ambiguous.GuideName}' cannot be synced because multiple CFs "
                + $"match this name: {matchList}. Delete or rename duplicate CFs in the service";
            publisher.AddError(message);
        }

        return transactions.AmbiguousCustomFormats.Count > 0;
    }

    private static void LogReplacedCustomFormats(
        IPipelinePublisher publisher,
        CustomFormatTransactionData transactions
    )
    {
        foreach (var name in transactions.ReplacedCustomFormats)
        {
            var message =
                $"Custom Format '{name}' already existed in the service and was replaced by Recyclarr";
            publisher.AddWarning(message);
        }
    }
}
