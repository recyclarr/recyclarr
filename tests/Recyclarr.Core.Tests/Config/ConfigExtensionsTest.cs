// using Recyclarr.Config;
// using Recyclarr.Config.Models;
// using Recyclarr.Config.Parsing;
// using Recyclarr.TrashGuide;
//
// namespace Recyclarr.Core.Tests.Config;
//
// [TestFixture]
// public class ConfigExtensionsTest
// {
//     [Test]
//     public void Filter_invalid_instances()
//     {
//         var configs = new[]
//         {
//             new LoadedConfigYaml("valid_NAME", SupportedServices.Sonarr, new ServiceConfigYaml()),
//         };
//
//         // Comparison should be case-insensitive
//         var invalidInstanceNames = configs.GetNonExistentInstanceNames(
//             new ConfigFilterCriteria { Instances = ["valid_name", "invalid_name"] }
//         );
//
//         invalidInstanceNames.Should().BeEquivalentTo("invalid_name");
//     }
//
//     [Test]
//     public void Get_configs_matching_service_type_and_instance_name()
//     {
//         var configs = new IServiceConfiguration[]
//         {
//             new RadarrConfiguration { InstanceName = "radarr1" },
//             new RadarrConfiguration { InstanceName = "radarr2" },
//             new RadarrConfiguration { InstanceName = "radarr3" },
//             new RadarrConfiguration { InstanceName = "radarr4" },
//             new SonarrConfiguration { InstanceName = "sonarr1" },
//             new SonarrConfiguration { InstanceName = "sonarr2" },
//             new SonarrConfiguration { InstanceName = "sonarr3" },
//             new SonarrConfiguration { InstanceName = "sonarr4" },
//         };
//
//         var result = configs.GetConfigsBasedOnSettings(
//             new ConfigFilterCriteria
//             {
//                 Service = SupportedServices.Radarr,
//                 Instances = ["radarr2", "radarr4", "radarr5", "sonarr2"],
//             }
//         );
//
//         result.Select(x => x.InstanceName).Should().BeEquivalentTo("radarr2", "radarr4");
//     }
//
//     [Test]
//     public void Get_configs_based_on_settings_with_empty_instances()
//     {
//         var configs = new IServiceConfiguration[]
//         {
//             new RadarrConfiguration { InstanceName = "radarr1" },
//             new SonarrConfiguration { InstanceName = "sonarr1" },
//         };
//
//         var result = configs.GetConfigsBasedOnSettings(
//             new ConfigFilterCriteria { Instances = Array.Empty<string>() }
//         );
//
//         result.Select(x => x.InstanceName).Should().BeEquivalentTo("radarr1", "sonarr1");
//     }
//
//     [Test]
//     public void Get_split_instance_names()
//     {
//         var configs = new IServiceConfiguration[]
//         {
//             new RadarrConfiguration
//             {
//                 InstanceName = "radarr1",
//                 BaseUrl = new Uri("http://radarr1"),
//             },
//             new RadarrConfiguration
//             {
//                 InstanceName = "radarr2",
//                 BaseUrl = new Uri("http://radarr1"),
//             },
//             new RadarrConfiguration
//             {
//                 InstanceName = "radarr3",
//                 BaseUrl = new Uri("http://radarr3"),
//             },
//             new RadarrConfiguration
//             {
//                 InstanceName = "radarr4",
//                 BaseUrl = new Uri("http://radarr4"),
//             },
//             new SonarrConfiguration
//             {
//                 InstanceName = "sonarr1",
//                 BaseUrl = new Uri("http://sonarr1"),
//             },
//             new SonarrConfiguration
//             {
//                 InstanceName = "sonarr2",
//                 BaseUrl = new Uri("http://sonarr2"),
//             },
//             new SonarrConfiguration
//             {
//                 InstanceName = "sonarr3",
//                 BaseUrl = new Uri("http://sonarr2"),
//             },
//             new SonarrConfiguration
//             {
//                 InstanceName = "sonarr4",
//                 BaseUrl = new Uri("http://sonarr4"),
//             },
//         };
//
//         var result = configs.GetSplitInstances();
//
//         result.Should().BeEquivalentTo("radarr1", "radarr2", "sonarr2", "sonarr3");
//     }
//
//     [Test]
//     public void Get_duplicate_instance_names()
//     {
//         var configs = new IServiceConfiguration[]
//         {
//             new RadarrConfiguration { InstanceName = "radarr1" },
//             new RadarrConfiguration { InstanceName = "radarr2" },
//             new RadarrConfiguration { InstanceName = "radarr2" },
//             new RadarrConfiguration { InstanceName = "radarr3" },
//             new SonarrConfiguration { InstanceName = "sonarr1" },
//             new SonarrConfiguration { InstanceName = "sonarr1" },
//         };
//
//         var result = configs.GetDuplicateInstanceNames();
//
//         result.Should().BeEquivalentTo("radarr2", "sonarr1");
//     }
// }
