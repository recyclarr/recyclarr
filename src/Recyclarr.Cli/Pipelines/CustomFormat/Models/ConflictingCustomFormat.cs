using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

internal record ConflictingCustomFormat(CustomFormatData GuideCf, int ConflictingId);
