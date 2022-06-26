using Common.Extensions;
using TrashLib.Radarr.CustomFormat.Guide;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors;

public class CustomFormatLookup
{
    private readonly ICustomFormatGroupParser _parser;
    private readonly IRadarrGuideService _guide;

    public CustomFormatLookup(ICustomFormatGroupParser parser, IRadarrGuideService guide)
    {
        _parser = parser;
        _guide = guide;
    }

    private static CustomFormatData? MatchDataWithCellEntry(ICollection<CustomFormatData> guideCfs,
        CustomFormatGroupItem groupItem)
    {
        return guideCfs.FirstOrDefault(x => x.FileName.EqualsIgnoreCase(groupItem.Anchor)) ??
               guideCfs.FirstOrDefault(x => x.Name.EqualsIgnoreCase(groupItem.Name));
    }

    public Dictionary<string, List<CustomFormatData>> MapAllCustomFormats()
    {
        var guideCfs = _guide.GetCustomFormatData();
        var groups = _parser.Parse();

        return groups.ToDictionary(
            x => x.Key,
            x => x.Value.Select(y => MatchDataWithCellEntry(guideCfs, y)).NotNull().ToList());
    }
}
