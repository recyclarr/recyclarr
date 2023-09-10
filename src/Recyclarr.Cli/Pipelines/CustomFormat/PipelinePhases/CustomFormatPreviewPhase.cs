namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatPreviewPhase
{
    private readonly ILogger _log;

    public CustomFormatPreviewPhase(ILogger log)
    {
        _log = log;
    }

    public void Execute(CustomFormatTransactionData transactions)
    {
        foreach (var (guideCf, conflictingId) in transactions.ConflictingCustomFormats)
        {
            _log.Warning(
                "Custom Format with name {Name} (Trash ID: {TrashId}) will be skipped because another " +
                "CF already exists with that name (ID: {ConflictId}). To fix the conflict, delete or " +
                "rename the CF with the mentioned name",
                guideCf.Name, guideCf.TrashId, conflictingId);
        }

        var created = transactions.NewCustomFormats;
        if (created.Count > 0)
        {
            _log.Information("Created {Count} New Custom Formats", created.Count);

            foreach (var cf in created)
            {
                _log.Debug("> Created: {TrashId} ({Name})", cf.TrashId, cf.Name);
            }
        }

        var updated = transactions.UpdatedCustomFormats;
        if (updated.Count > 0)
        {
            _log.Information("Updated {Count} Existing Custom Formats", updated.Count);

            foreach (var cf in updated)
            {
                _log.Debug("> Updated: {TrashId} ({Name})", cf.TrashId, cf.Name);
            }
        }

        var skipped = transactions.UnchangedCustomFormats;
        if (skipped.Count > 0)
        {
            _log.Information("Skipped {Count} Custom Formats that did not change", skipped.Count);
            _log.Debug("Custom Formats Skipped: {CustomFormats}",
                skipped.ToDictionary(k => k.TrashId, v => v.Name));

            // Do not print skipped CFs to console; they are too verbose
        }

        var deleted = transactions.DeletedCustomFormats;
        if (deleted.Count > 0)
        {
            _log.Information("Deleted {Count} Custom Formats", deleted.Count);

            foreach (var mapping in deleted)
            {
                _log.Debug("> Deleted: {TrashId} ({CustomFormatName})", mapping.TrashId, mapping.CustomFormatName);
            }
        }

        var totalCount = created.Count + updated.Count + deleted.Count;
        if (totalCount > 0)
        {
            _log.Information("Total of {Count} custom formats were synced", totalCount);
        }
        else
        {
            _log.Information("All custom formats are already up to date!");
        }
    }
}
