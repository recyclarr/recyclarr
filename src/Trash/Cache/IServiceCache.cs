namespace Trash.Cache
{
    public interface IServiceCache
    {
        T Load<T>();
        void Save<T>(T obj);
    }
}
