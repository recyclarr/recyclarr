namespace Recyclarr.Servarr.CustomFormat;

public record CustomFormatDeleteSummary(int Deleted, int Failed, IReadOnlyList<string> FailedNames);
