using Recyclarr.Cli.Processors.Config;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashLib.Config.Listers;

namespace Recyclarr.Cli.Tests.Processors;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigListProcessorTest
{
    [Test]
    [InlineAutoMockData(ConfigCategory.Templates)]
    public void List_templates_invokes_correct_lister(
        ConfigCategory category,
        [Frozen(Matching.ImplementedInterfaces)] StubAutofacIndex<ConfigCategory, IConfigLister> configListers,
        IConfigLister lister,
        ConfigListProcessor sut)
    {
        configListers.Add(category, lister);

        sut.Process(category);

        lister.Received().List();
    }
}
