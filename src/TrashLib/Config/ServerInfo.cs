using Flurl;
using Flurl.Http;
using Serilog;
using TrashLib.Extensions;

namespace TrashLib.Config
{
    internal class ServerInfo : IServerInfo
    {
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly ILogger _log;

        public ServerInfo(string baseUrl, string apiKey, ILogger log)
        {
            _baseUrl = baseUrl;
            _apiKey = apiKey;
            _log = log;
        }

        public IFlurlRequest BuildRequest()
        {
            return _baseUrl
                .AppendPathSegment("api/v3")
                .SetQueryParams(new {apikey = _apiKey})
                .SanitizedLogging(_log);
        }
    }
}
