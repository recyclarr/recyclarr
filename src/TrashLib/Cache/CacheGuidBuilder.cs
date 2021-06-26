using System.Data.HashFunction.FNV;
using System.Text;
using TrashLib.Config;

namespace TrashLib.Cache
{
    internal class CacheGuidBuilder : ICacheGuidBuilder
    {
        private readonly IFNV1a _hash;

        public CacheGuidBuilder()
        {
            _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(32));
        }

        public string MakeGuid(IServiceConfiguration config)
        {
            return _hash
                .ComputeHash(Encoding.ASCII.GetBytes(config.BaseUrl))
                .AsHexString();
        }
    }
}
