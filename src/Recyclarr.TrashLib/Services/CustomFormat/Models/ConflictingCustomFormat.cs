namespace Recyclarr.TrashLib.Services.CustomFormat.Models;

public record ConflictingCustomFormat(
    ProcessedCustomFormatData GuideCf,
    int ConflictingId
);
