namespace Trash.Command
{
    public interface IActiveServiceCommandProvider
    {
        IServiceCommand ActiveCommand { get; set; }
    }
}
