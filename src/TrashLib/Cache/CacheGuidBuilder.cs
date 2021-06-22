using System.Data.HashFunction.FNV;
using System.Text;
using TrashLib.Config;

namespace TrashLib.Cache
{
    internal class CacheGuidBuilder : ICacheGuidBuilder
    {
        private readonly string _baseUrl;
        private readonly IFNV1a _hash;

        public CacheGuidBuilder(IServiceConfiguration config)
        {
            _baseUrl = config.BaseUrl;
            _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(32));
        }

        public string MakeGuid()
        {
            return _hash
                .ComputeHash(Encoding.ASCII.GetBytes(_baseUrl))
                .AsHexString();
        }
    }
}
