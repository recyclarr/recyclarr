using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Core.TestLibrary;

public static class NewCf
{
    public static CustomFormatResource DataWithScore(
        string name,
        string trashId,
        int score,
        int id = 0
    )
    {
        return new CustomFormatResource
        {
            Id = id,
            Name = name,
            TrashId = trashId,
            TrashScores = { ["default"] = score },
        };
    }

    public static CustomFormatResource DataWithScores(
        string name,
        string trashId,
        int id,
        params (string ScoreSet, int Score)[] scores
    )
    {
        return new CustomFormatResource
        {
            Id = id,
            Name = name,
            TrashId = trashId,
            TrashScores = scores.ToDictionary(x => x.ScoreSet, x => x.Score),
        };
    }

    public static CustomFormatResource Data(string name, string trashId, int id = 0)
    {
        return new CustomFormatResource
        {
            Id = id,
            Name = name,
            TrashId = trashId,
        };
    }

    public static RadarrCustomFormatResource RadarrData(string name, string trashId, int id = 0)
    {
        return new RadarrCustomFormatResource
        {
            Id = id,
            Name = name,
            TrashId = trashId,
        };
    }

    public static SonarrCustomFormatResource SonarrData(string name, string trashId, int id = 0)
    {
        return new SonarrCustomFormatResource
        {
            Id = id,
            Name = name,
            TrashId = trashId,
        };
    }
}
