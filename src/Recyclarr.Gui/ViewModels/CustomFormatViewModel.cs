using ReactiveUI;
using TrashLib.Radarr.CustomFormat.Models;

namespace Recyclarr.Gui.ViewModels;

public class CustomFormatGroupViewModel : ReactiveObject
{
    public string GroupName { get; set; }
    public ICollection<CustomFormatGroupItem> CustomFormats { get; set; }

    public CustomFormatGroupViewModel()
    {
    }
}

public class CustomFormatViewModel : ReactiveObject
{
    public CustomFormatViewModel()
    {
    }
}
