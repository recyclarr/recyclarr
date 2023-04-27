using Autofac.Features.Indexed;
using Recyclarr.TrashLib.Config.Listers;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigAutofacModuleTest : TrashLibIntegrationFixture
{
    private static IEnumerable<ConfigListCategory> AllConfigListCategories()
    {
        return Enum.GetValues<ConfigListCategory>();
    }

    [TestCaseSource(nameof(AllConfigListCategories))]
    public void All_list_category_types_registered(ConfigListCategory category)
    {
        var sut = Resolve<IIndex<ConfigListCategory, IConfigLister>>();
        var result = sut.TryGetValue(category, out _);
        result.Should().BeTrue();
    }
}
