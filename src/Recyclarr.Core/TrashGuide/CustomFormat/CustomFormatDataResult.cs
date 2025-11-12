using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.TrashGuide.CustomFormat;

public record CustomFormatDataResult(ICollection<CustomFormatResource> CustomFormats);
