using Recyclarr.Pipelines.CustomFormat.Models;
using Recyclarr.Sync;
using Recyclarr.Sync.Progress;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.CustomFormat;

internal class CustomFormatTransactionLogger(ILogger log)
{
    public void LogTransactions(
        CustomFormatTransactionData transactions,
        IPipelinePublisher publisher,
        CustomFormatPipelineResult result
    )
    {
        LogDiagnostics(publisher, transactions);
        LogResults(transactions);
        SetStatus(transactions, publisher, result);
    }

    public static void SetStatus(
        CustomFormatTransactionData transactions,
        IPipelinePublisher publisher,
        CustomFormatPipelineResult result
    )
    {
        var status = result.Status switch
        {
            SyncResultStatus.Succeeded => PipelineProgressStatus.Succeeded,
            SyncResultStatus.Partial => PipelineProgressStatus.Partial,
            SyncResultStatus.Failed => PipelineProgressStatus.Failed,
            SyncResultStatus.Blocked => PipelineProgressStatus.Skipped,
            _ => throw new ArgumentOutOfRangeException(nameof(result)),
        };
        publisher.SetStatus(
            status,
            transactions.TotalCustomFormatChanges,
            BuildItemChanges(transactions)
        );
    }

    private static PipelineItemChanges BuildItemChanges(CustomFormatTransactionData transactions)
    {
        var created = transactions
            .NewCustomFormats.Select(cf => cf.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var updated = transactions
            .UpdatedCustomFormats.Select(cf => cf.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var deleted = transactions
            .DeletedCustomFormats.Select(m => m.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return new PipelineItemChanges(created, updated, deleted);
    }

    private void LogResults(CustomFormatTransactionData transactions)
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
    }

    private static void LogDiagnostics(
        IPipelinePublisher publisher,
        CustomFormatTransactionData transactions
    )
    {
        LogReplacedCustomFormats(publisher, transactions);

        foreach (var ambiguous in transactions.AmbiguousCustomFormats)
        {
            publisher.Add(
                new AmbiguousCustomFormatOutcome(ambiguous.GuideName, ambiguous.ServiceMatches)
            );
        }
    }

    private static void LogReplacedCustomFormats(
        IPipelinePublisher publisher,
        CustomFormatTransactionData transactions
    )
    {
        var replaced = transactions.ReplacedCustomFormats;
        if (replaced.Count == 0)
        {
            return;
        }

        publisher.Add(new ReplacedCustomFormatsOutcome(replaced.ToList()));
    }
}
