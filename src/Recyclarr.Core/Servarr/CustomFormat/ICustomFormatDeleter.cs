namespace Recyclarr.Servarr.CustomFormat;

public interface ICustomFormatDeleter
{
    Task<IReadOnlyList<CustomFormatDeleteItem>> GetCandidatesAsync(
        IDeleteCustomFormatSettings settings,
        CancellationToken ct
    );

    Task<CustomFormatDeleteSummary> DeleteAsync(
        IReadOnlyList<CustomFormatDeleteItem> candidates,
        CancellationToken ct
    );
}
