using Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Guide;

public interface ICustomFormatParser
{
    CustomFormatData ParseCustomFormatData(string guideData, string fileName);
}
