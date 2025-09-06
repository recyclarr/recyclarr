namespace Recyclarr.TrashGuide.CustomFormat;

public record DuplicateCustomFormatInfo(
    string TrashId,
    IReadOnlyList<string> Names,
    IReadOnlyList<string> Sources
);
