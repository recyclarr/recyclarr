using Autofac.Features.Indexed;
using Recyclarr.TrashLib.Config.Listers;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigAutofacModuleTest : TrashLibIntegrationFixture
{
    private static IEnumerable<ConfigCategory> AllConfigListCategories()
    {
        return Enum.GetValues<ConfigCategory>();
    }

    [TestCaseSource(nameof(AllConfigListCategories))]
    public void All_list_category_types_registered(ConfigCategory category)
    {
        var sut = Resolve<IIndex<ConfigCategory, IConfigLister>>();
        var result = sut.TryGetValue(category, out _);
        result.Should().BeTrue();
    }
}
