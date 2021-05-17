namespace Recyclarr.Code.Radarr
{
    public class SelectableItem<T>
    {
        public SelectableItem(T item)
        {
            Item = item;
        }

        public T Item { get; }
        public bool Selected { get; set; }
    }
}
