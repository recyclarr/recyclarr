namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

public record ConflictingCustomFormat(
    CustomFormatData GuideCf,
    int ConflictingId
);
