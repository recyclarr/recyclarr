using Flurl;

namespace TrashLib.Config
{
    internal class ServerInfo : IServerInfo
    {
        private readonly IConfigurationProvider _configProvider;
        public string ApiKey => _configProvider.ActiveConfiguration.ApiKey;
        public string BaseUrl => _configProvider.ActiveConfiguration.BaseUrl;

        public ServerInfo(IConfigurationProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public string BuildUrl()
        {
            return BaseUrl
                .AppendPathSegment("api/v3")
                .SetQueryParams(new {apikey = ApiKey});
        }
    }
}
