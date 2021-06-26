using TrashLib.Config;

namespace TrashLib.Cache
{
    public interface ICacheGuidBuilder
    {
        string MakeGuid(IServiceConfiguration config);
    }
}
