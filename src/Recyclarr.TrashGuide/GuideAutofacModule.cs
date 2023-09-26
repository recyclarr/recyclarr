using Autofac;
using Recyclarr.TrashGuide.CustomFormat;
using Recyclarr.TrashGuide.MediaNaming;
using Recyclarr.TrashGuide.QualitySize;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.TrashGuide;

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

        // Media Naming
        builder.RegisterType<MediaNamingGuideService>().As<IMediaNamingGuideService>();
    }
}
