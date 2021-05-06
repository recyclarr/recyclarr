using System.Threading.Tasks;
using Trash.Command;

namespace Trash.Radarr.CustomFormat
{
    public interface ICustomFormatUpdater
    {
        Task Process(IServiceCommand args, RadarrConfiguration config);
    }
}
