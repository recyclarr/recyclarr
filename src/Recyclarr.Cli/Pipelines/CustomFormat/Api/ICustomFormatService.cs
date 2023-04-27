using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Api;

public interface ICustomFormatService
{
    Task<IList<CustomFormatData>> GetCustomFormats(IServiceConfiguration config);
    Task<CustomFormatData?> CreateCustomFormat(IServiceConfiguration config, CustomFormatData cf);
    Task UpdateCustomFormat(IServiceConfiguration config, CustomFormatData cf);
    Task DeleteCustomFormat(IServiceConfiguration config, int customFormatId);
}
