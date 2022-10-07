using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Guide;

public interface ICustomFormatParser
{
    CustomFormatData ParseCustomFormatData(string guideData, string fileName);
}
