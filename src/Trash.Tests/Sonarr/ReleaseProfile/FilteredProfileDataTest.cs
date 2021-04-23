using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Trash.Sonarr;
using Trash.Sonarr.ReleaseProfile;

namespace Trash.Tests.Sonarr.ReleaseProfile
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class FilteredProfileDataTest
    {
        [Test]
        public void Filter_IncludeOptional_HasAllOptionalItems()
        {
            var config = new ReleaseProfileConfig();
            config.Filter.IncludeOptional = true;

            var profileData = new ProfileData
            {
                Ignored = new List<string> {"ignored1"},
                Required = new List<string> {"required1"},
                Preferred = new Dictionary<int, List<string>>
                {
                    {100, new List<string> {"preferred1"}}
                },
                Optional = new ProfileDataOptional
                {
                    Ignored = new List<string> {"ignored2"},
                    Required = new List<string> {"required2"},
                    Preferred = new Dictionary<int, List<string>>
                    {
                        {200, new List<string> {"preferred2"}},
                        {100, new List<string> {"preferred3"}}
                    }
                }
            };

            var filtered = new FilteredProfileData(profileData, config);

            filtered.Should().BeEquivalentTo(new
            {
                Ignored = new List<string> {"ignored1", "ignored2"},
                Required = new List<string> {"required1", "required2"},
                Preferred = new Dictionary<int, List<string>>
                {
                    {100, new List<string> {"preferred1", "preferred3"}},
                    {200, new List<string> {"preferred2"}}
                }
            });
        }

        [Test]
        public void Filter_ExcludeOptional_HasNoOptionalItems()
        {
            var config = new ReleaseProfileConfig();
            config.Filter.IncludeOptional = false;

            var profileData = new ProfileData
            {
                Ignored = new List<string> {"ignored1"},
                Required = new List<string> {"required1"},
                Preferred = new Dictionary<int, List<string>>
                {
                    {100, new List<string> {"preferred1"}}
                },
                Optional = new ProfileDataOptional
                {
                    Ignored = new List<string> {"ignored2"},
                    Required = new List<string> {"required2"},
                    Preferred = new Dictionary<int, List<string>>
                    {
                        {200, new List<string> {"preferred2"}},
                        {100, new List<string> {"preferred3"}}
                    }
                }
            };

            var filtered = new FilteredProfileData(profileData, config);

            filtered.Should().BeEquivalentTo(new
            {
                Ignored = new List<string> {"ignored1"},
                Required = new List<string> {"required1"},
                Preferred = new Dictionary<int, List<string>>
                {
                    {100, new List<string> {"preferred1"}}
                }
            });
        }
    }
}
