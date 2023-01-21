using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.TestLibrary;

public static class CfTestUtils
{
    public static QualityProfileCustomFormatScoreMapping NewMapping(params FormatMappingEntry[] entries)
    {
        return new(false) {Mapping = entries.ToList()};
    }

    public static QualityProfileCustomFormatScoreMapping NewMappingWithReset(params FormatMappingEntry[] entries)
    {
        return new(true) {Mapping = entries.ToList()};
    }
}
