using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

public record ConflictingCustomFormat(CustomFormatData GuideCf, int ConflictingId);
