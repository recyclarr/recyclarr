namespace Recyclarr.Command;

public interface IRadarrCommand : IServiceCommand
{
    bool ListCustomFormats { get; }
}
