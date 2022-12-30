using AutoMapper;

namespace Recyclarr.TrashLib.Startup;

public static class AutoMapperConfig
{
    public static IMapper Setup()
    {
        // todo: consider using AutoMapper.Contrib.Autofac.DependencyInjection
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(AutoMapperConfig));
        });

    #if DEBUG
        mapperConfig.AssertConfigurationIsValid();
    #endif

        return mapperConfig.CreateMapper();
    }
}
