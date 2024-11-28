using Recyclarr.Notifications;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class CustomFormatTransactionLogger(ILogger log, NotificationEmitter notify)
{
    public void LogTransactions(CustomFormatPipelineContext context)
    {
        var transactions = context.TransactionOutput;

        foreach (var (guideCf, conflictingId) in transactions.ConflictingCustomFormats)
        {
            log.Warning(
                "Custom Format with name {Name} (Trash ID: {TrashId}) will be skipped because another "
                    + "CF already exists with that name (ID: {ConflictId}). To fix the conflict, delete or "
                    + "rename the CF with the mentioned name",
                guideCf.Name,
                guideCf.TrashId,
                conflictingId
            );
        }

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
                log.Debug(
                    "> Deleted: {TrashId} ({CustomFormatName})",
                    mapping.TrashId,
                    mapping.CustomFormatName
                );
            }
        }

        var totalCount = transactions.TotalCustomFormatChanges;
        if (totalCount > 0)
        {
            log.Information("Total of {Count} custom formats were synced", totalCount);
            notify.SendStatistic("Custom Formats Synced", totalCount);
        }
        else
        {
            log.Information("All custom formats are already up to date!");
        }

        // Logging is done (and shared with) in CustomFormatPreviewPhase
    }
}
