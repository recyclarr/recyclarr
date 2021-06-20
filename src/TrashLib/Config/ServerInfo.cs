using Flurl;

namespace TrashLib.Config
{
    internal class ServerInfo : IServerInfo
    {
        private readonly IConfigProvider _configProvider;

        public ServerInfo(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public string ApiKey => _configProvider.Active.ApiKey;
        public string BaseUrl => _configProvider.Active.BaseUrl;

        public string BuildUrl()
        {
            return BaseUrl
                .AppendPathSegment("api/v3")
                .SetQueryParams(new {apikey = ApiKey});
        }
    }
}
