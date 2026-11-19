namespace Recyclarr.Cli.Processors.Sync.Results;

internal enum ChangeKind
{
    Create,
    Update,
    Delete,
}

/// <summary>
/// One resource a sync would change, in the single shape every pipeline is reduced to for the
/// preview. <see cref="Detail"/> is a short muted explanation shown beside the name (where a
/// custom format came from); <see cref="Fields"/> and <see cref="Sections"/> hold the changed
/// values, the latter grouped when one resource has several kinds of change (quality profiles).
/// </summary>
internal sealed record PreviewChange(
    ChangeKind Kind,
    string Name,
    string? Detail,
    IReadOnlyList<FieldChange> Fields,
    IReadOnlyList<PreviewSection> Sections
);

/// <summary>
/// A titled group of changes under one resource. <see cref="Layout"/> is used instead of
/// <see cref="Fields"/> for a quality list, which only reads correctly as a whole.
/// <see cref="Note"/> is shown muted beside the title (where the values came from).
/// </summary>
internal sealed record PreviewSection(
    string Title,
    string? Note,
    IReadOnlyList<FieldChange> Fields,
    IReadOnlyList<QualityLine> Layout
);

/// <summary>
/// One entry of a profile's desired quality list, in display order. <see cref="Note"/> says how
/// the entry differs from the current list; <see cref="MovedUp"/> is set only for an entry that
/// moved, telling its direction.
/// </summary>
internal sealed record QualityLine(
    string Name,
    bool Allowed,
    string? Note,
    bool? MovedUp,
    IReadOnlyList<string> Members
);

/// <summary>Why a pipeline shows no changes although it did not complete normally.</summary>
internal sealed record PreviewSkip(string Label, string Reason);

internal sealed record PreviewPipeline(
    string Title,
    IReadOnlyList<PreviewChange> Changes,
    PreviewSkip? Skip
);
