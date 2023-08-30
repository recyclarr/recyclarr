namespace Recyclarr.TrashLib.Config.Services;

public interface IConfigTemplateGuideService
{
    IReadOnlyCollection<TemplatePath> LoadTemplateData();
}
