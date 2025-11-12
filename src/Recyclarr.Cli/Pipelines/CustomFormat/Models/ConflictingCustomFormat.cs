using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

internal record ConflictingCustomFormat(CustomFormatResource GuideCf, int ConflictingId);
