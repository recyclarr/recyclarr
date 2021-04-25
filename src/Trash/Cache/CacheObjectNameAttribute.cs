using System;

namespace Trash.Cache
{
    public class CacheObjectNameAttribute : Attribute
    {
        public CacheObjectNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
