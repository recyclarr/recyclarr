namespace Recyclarr.TrashLib.Config.Services;

public interface IConfigTemplateGuideService
{
    Task<IReadOnlyCollection<TemplatePath>> LoadTemplateData();
}
