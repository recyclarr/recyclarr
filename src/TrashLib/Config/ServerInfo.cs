using Flurl;

namespace TrashLib.Config
{
    internal class ServerInfo : IServerInfo
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public ServerInfo(string baseUrl, string apiKey)
        {
            _baseUrl = baseUrl;
            _apiKey = apiKey;
        }

        public string BuildUrl()
        {
            return _baseUrl
                .AppendPathSegment("api/v3")
                .SetQueryParams(new {apikey = _apiKey});
        }
    }
}
