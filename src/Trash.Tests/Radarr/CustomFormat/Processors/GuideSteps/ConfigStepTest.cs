using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Trash.Radarr;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;
using Trash.Radarr.CustomFormat.Processors.GuideSteps;

namespace Trash.Tests.Radarr.CustomFormat.Processors.GuideSteps
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ConfigStepTest
    {
        [Test]
        public void All_custom_formats_found_in_guide()
        {
            var testProcessedCfs = new List<ProcessedCustomFormatData>
            {
                new("name1", "id1", JObject.FromObject(new {name = "name1"}))
                {
                    Score = 100
                },
                new("name3", "id3", JObject.FromObject(new {name = "name3"}))
            };

            var testConfig = new CustomFormatConfig[]
            {
                new()
                {
                    Names = new List<string> {"name1", "name3"},
                    QualityProfiles = new List<QualityProfileConfig>
                    {
                        new() {Name = "profile1", Score = 50}
                    }
                }
            };

            var processor = new ConfigStep();
            processor.Process(testProcessedCfs, testConfig);

            processor.RenamedCustomFormats.Should().BeEmpty();
            processor.CustomFormatsNotInGuide.Should().BeEmpty();
            processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
            {
                new()
                {
                    CustomFormats = testProcessedCfs,
                    QualityProfiles = testConfig[0].QualityProfiles
                }
            }, op => op
                .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
                .WhenTypeIs<JToken>());
        }

        [Test]
        public void Cache_names_are_used_instead_of_name_in_json_data()
        {
            var testProcessedCfs = new List<ProcessedCustomFormatData>
            {
                new("name1", "id1", JObject.FromObject(new {name = "name1"}))
                {
                    Score = 100
                },
                new("name3", "id3", JObject.FromObject(new {name = "name3"}))
                {
                    CacheEntry = new TrashIdMapping("id3", "name1")
                }
            };

            var testConfig = new CustomFormatConfig[]
            {
                new()
                {
                    Names = new List<string> {"name1"}
                }
            };

            var processor = new ConfigStep();
            processor.Process(testProcessedCfs, testConfig);

            processor.CustomFormatsNotInGuide.Should().BeEmpty();
            processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
            {
                new()
                {
                    CustomFormats = new List<ProcessedCustomFormatData>
                        {testProcessedCfs[1]}
                }
            }, op => op
                .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
                .WhenTypeIs<JToken>());
        }

        [Test]
        public void Custom_formats_missing_from_config_are_skipped()
        {
            var testProcessedCfs = new List<ProcessedCustomFormatData>
            {
                new("name1", "", new JObject()),
                new("name2", "", new JObject())
            };

            var testConfig = new CustomFormatConfig[]
            {
                new()
                {
                    Names = new List<string> {"name1"}
                }
            };

            var processor = new ConfigStep();
            processor.Process(testProcessedCfs, testConfig);

            processor.RenamedCustomFormats.Should().BeEmpty();
            processor.CustomFormatsNotInGuide.Should().BeEmpty();
            processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
            {
                new()
                {
                    CustomFormats = new List<ProcessedCustomFormatData>
                    {
                        new("name1", "", new JObject())
                    }
                }
            }, op => op
                .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
                .WhenTypeIs<JToken>());
        }

        [Test]
        public void Custom_formats_missing_from_guide_are_added_to_not_in_guide_list()
        {
            var testProcessedCfs = new List<ProcessedCustomFormatData>
            {
                new("name1", "", new JObject()),
                new("name2", "", new JObject())
            };

            var testConfig = new CustomFormatConfig[]
            {
                new()
                {
                    Names = new List<string> {"name1", "name3"}
                }
            };

            var processor = new ConfigStep();
            processor.Process(testProcessedCfs, testConfig);

            processor.RenamedCustomFormats.Should().BeEmpty();
            processor.CustomFormatsNotInGuide.Should().BeEquivalentTo(new List<string> {"name3"}, op => op
                .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
                .WhenTypeIs<JToken>());
            processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
            {
                new()
                {
                    CustomFormats = new List<ProcessedCustomFormatData>
                    {
                        new("name1", "", new JObject())
                    }
                }
            }, op => op
                .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
                .WhenTypeIs<JToken>());
        }

        [Test]
        public void Custom_formats_with_same_trash_id_and_same_name_in_cache_are_in_renamed_list()
        {
            var testProcessedCfs = new List<ProcessedCustomFormatData>
            {
                new("name1", "id1", new JObject())
                {
                    CacheEntry = new TrashIdMapping("id1", "name2")
                },
                new("name2", "id2", new JObject())
                {
                    CacheEntry = new TrashIdMapping("id2", "name1")
                }
            };

            var testConfig = new CustomFormatConfig[]
            {
                new()
                {
                    Names = new List<string> {"name1", "name2"}
                }
            };

            var processor = new ConfigStep();
            processor.Process(testProcessedCfs, testConfig);

            processor.RenamedCustomFormats.Should().BeEquivalentTo(testProcessedCfs, op => op
                .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
                .WhenTypeIs<JToken>());
            processor.CustomFormatsNotInGuide.Should().BeEmpty();
            processor.ConfigData.Should().BeEquivalentTo(new List<ProcessedConfigData>
            {
                new()
                {
                    CustomFormats = testProcessedCfs
                }
            }, op => op
                .Using<JToken>(jctx => jctx.Subject.Should().BeEquivalentTo(jctx.Expectation))
                .WhenTypeIs<JToken>());
        }
    }
}
