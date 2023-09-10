namespace Recyclarr.TrashLib.Guide;

public interface IConfigTemplateGuideService
{
    IReadOnlyCollection<TemplatePath> GetTemplateData();
    IReadOnlyCollection<TemplatePath> GetIncludeData();
}
