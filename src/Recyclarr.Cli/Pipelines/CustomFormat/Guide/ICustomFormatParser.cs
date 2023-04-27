using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Guide;

public interface ICustomFormatParser
{
    CustomFormatData ParseCustomFormatData(string guideData, string fileName);
}
