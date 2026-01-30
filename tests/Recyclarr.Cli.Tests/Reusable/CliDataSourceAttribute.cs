using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Cli.Tests.Reusable;

/// <summary>
/// TUnit data source attribute for CLI integration tests.
/// Uses full CompositionRoot.Setup() which is a superset of CoreAutofacModule.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1813:Avoid unsealed attributes",
    Justification = "May need to be inheritable"
)]
internal sealed class CliDataSourceAttribute : CoreDataSourceAttribute
{
    protected override void RegisterTypes(ContainerBuilder builder)
    {
        // Do NOT invoke base - CompositionRoot.Setup() is a superset that includes CoreAutofacModule
        CompositionRoot.Setup(builder);
    }
}
