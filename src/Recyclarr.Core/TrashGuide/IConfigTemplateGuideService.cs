namespace Recyclarr.TrashGuide;

public interface IConfigTemplateGuideService
{
    IReadOnlyCollection<TemplatePath> GetTemplateData();
    IReadOnlyCollection<TemplatePath> GetIncludeData();
}
