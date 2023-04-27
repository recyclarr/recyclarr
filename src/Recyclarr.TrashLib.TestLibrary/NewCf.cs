using Recyclarr.TrashLib.Models;

namespace Recyclarr.TrashLib.TestLibrary;

public static class NewCf
{
    public static CustomFormatData DataWithScore(string name, string trashId, int score, int id = 0)
    {
        return new CustomFormatData
        {
            Id = id,
            Name = name,
            TrashId = trashId,
            TrashScore = score
        };
    }

    public static CustomFormatData Data(string name, string trashId, int id = 0)
    {
        return new CustomFormatData
        {
            Id = id,
            Name = name,
            TrashId = trashId
        };
    }
}
