using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.TestLibrary;

public static class CfTestUtils
{
    public static QualityProfileCustomFormatScoreMapping NewMapping(params FormatMappingEntry[] entries)
    {
        return new QualityProfileCustomFormatScoreMapping(false) {Mapping = entries.ToList()};
    }

    public static QualityProfileCustomFormatScoreMapping NewMappingWithReset(params FormatMappingEntry[] entries)
    {
        return new QualityProfileCustomFormatScoreMapping(true) {Mapping = entries.ToList()};
    }
}
