using System;
using TrashLib.Cache;
using TrashLib.Config;

namespace TrashLib.Radarr.CustomFormat.Cache
{
    internal class CachePersisterFactory : ICachePersisterFactory
    {
        private readonly Func<IServiceConfiguration, ICacheGuidBuilder> _guidBuilderFactory;
        private readonly Func<ICacheGuidBuilder, ICachePersister> _persisterFactory;

        public CachePersisterFactory(
            Func<IServiceConfiguration, ICacheGuidBuilder> guidBuilderFactory,
            Func<ICacheGuidBuilder, ICachePersister> persisterFactory)
        {
            _guidBuilderFactory = guidBuilderFactory;
            _persisterFactory = persisterFactory;
        }

        public ICachePersister Create(IServiceConfiguration config)
        {
            return _persisterFactory(_guidBuilderFactory(config));
        }
    }
}
