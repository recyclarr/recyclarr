using Autofac;
using Recyclarr.TrashLib.Guide.CustomFormat;
using Recyclarr.TrashLib.Guide.QualitySize;
using Recyclarr.TrashLib.Guide.ReleaseProfile;

namespace Recyclarr.TrashLib.Guide;

public class GuideAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        builder.RegisterType<ConfigTemplateGuideService>().As<IConfigTemplateGuideService>().SingleInstance();

        // Custom Format
        builder.RegisterType<CustomFormatGuideService>().As<ICustomFormatGuideService>().SingleInstance();
        builder.RegisterType<CustomFormatLoader>().As<ICustomFormatLoader>();
        builder.RegisterType<CustomFormatCategoryParser>().As<ICustomFormatCategoryParser>();

        // Release Profile
        builder.RegisterType<ReleaseProfileGuideParser>();
        builder.RegisterType<ReleaseProfileGuideService>().As<IReleaseProfileGuideService>().SingleInstance();

        // Quality Size
        builder.RegisterType<QualitySizeGuideService>().As<IQualitySizeGuideService>().SingleInstance();
        builder.RegisterType<QualitySizeGuideParser>();
    }
}
