namespace Recyclarr.TrashLib.Config.Services;

public interface IConfigTemplateGuideService
{
    IReadOnlyCollection<TemplatePath> GetTemplateData();
    IReadOnlyCollection<TemplatePath> GetIncludeData();
}
