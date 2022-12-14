using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Guide;

public interface ICustomFormatParser
{
    CustomFormatData ParseCustomFormatData(string guideData, string fileName);
}
