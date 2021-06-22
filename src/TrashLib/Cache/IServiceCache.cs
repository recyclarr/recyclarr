namespace TrashLib.Cache
{
    public interface IServiceCache
    {
        T? Load<T>(ICacheGuidBuilder guidBuilder) where T : class;
        void Save<T>(T obj, ICacheGuidBuilder guidBuilder) where T : class;
    }
}
