using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Trash.Radarr.CustomFormat.Guide;

namespace Trash.Tests.Radarr.CustomFormat.Guide
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class GithubCustomFormatJsonRequesterTest
    {
        [Test]
        public async Task Requesting_json_from_github_works()
        {
            var requester = new GithubCustomFormatJsonRequester();

            var jsonList = (await requester.GetCustomFormatJson()).ToList();

            Action act = () => JObject.Parse(jsonList.First());

            // As of the time this test was written, there are around 58 custom format JSON files.
            // This number can fluctuate over time, but I'm only interested in verifying we get a handful
            // of files in the response.
            jsonList.Should().HaveCountGreaterOrEqualTo(5);

            act.Should().NotThrow();
        }
    }
}
