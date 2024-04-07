using Autofac;
using Recyclarr.Common.FluentValidation;

namespace Recyclarr.Common;

public class CommonAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<RuntimeValidationService>().As<IRuntimeValidationService>();
        builder.RegisterType<ValidationLogger>();
    }
}
