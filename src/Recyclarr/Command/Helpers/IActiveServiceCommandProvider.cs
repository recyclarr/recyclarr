namespace Recyclarr.Command.Helpers;

public interface IActiveServiceCommandProvider
{
    IServiceCommand ActiveCommand { get; set; }
}
