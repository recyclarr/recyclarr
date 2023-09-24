using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Tests.TestLibrary;

public static class NewCf
{
    public static CustomFormatData DataWithScore(string name, string trashId, int score, int id = 0)
    {
        return new CustomFormatData
        {
            Id = id,
            Name = name,
            TrashId = trashId,
            TrashScores = {["default"] = score}
        };
    }

    public static CustomFormatData DataWithScores(
        string name,
        string trashId,
        int id,
        params (string ScoreSet, int Score)[] scores)
    {
        return new CustomFormatData
        {
            Id = id,
            Name = name,
            TrashId = trashId,
            TrashScores = scores.ToDictionary(x => x.ScoreSet, x => x.Score)
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
