using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using TrashLib.Radarr.CustomFormat.Models;
using TrashLib.Radarr.CustomFormat.Processors;

namespace Recyclarr.Gui.ViewModels;

public class CustomFormatGroupViewModel : ReactiveObject
{
    public string GroupName { get; }
    public ICollection<CustomFormatData> CustomFormats { get; }

    public CustomFormatGroupViewModel(string groupName, ICollection<CustomFormatData> customFormats)
    {
        GroupName = groupName;
        CustomFormats = customFormats;
    }
}

public class CustomFormatViewModel : ReactiveObject
{
    private readonly ICustomFormatLookup _cfLookup;
    private readonly List<CustomFormatGroupViewModel> _groups = new();

    public List<CustomFormatGroupViewModel> Groups => _groups;
    public ReactiveCommand<Unit, Unit> OnInit;

    public CustomFormatViewModel(ICustomFormatLookup cfLookup)
    {
        _cfLookup = cfLookup;

        OnInit = ReactiveCommand.Create(() =>
        {
            foreach (var (groupName, cfs) in _cfLookup.MapAllCustomFormats())
            {
                
            }
        });
    }
}
