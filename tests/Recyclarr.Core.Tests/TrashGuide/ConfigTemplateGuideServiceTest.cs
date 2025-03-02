using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.TrashGuide;

internal sealed class ConfigTemplateGuideServiceTest
{
    [Test, AutoMockData]
    public void Throw_when_templates_dir_does_not_exist(ConfigTemplateGuideService sut)
    {
        var act = () => _ = sut.GetTemplateData();

        act.Should().Throw<InvalidDataException>().WithMessage("Recyclarr*templates*");
    }
}
