using Autofac;

namespace Recyclarr.TrashLib.Services.Common;

public class GuideServicesAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<GuideDataLister>().As<IGuideDataLister>();
    }
}
