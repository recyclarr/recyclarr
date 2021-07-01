using System;
using System.Collections.Generic;
using System.Linq;
using TrashLib.Radarr.Config;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors.GuideSteps
{
    internal class CustomFormatProcessor : ICustomFormatProcessor
    {
        public List<ProcessedCustomFormatData> CustomFormats { get; } = new();

        public void Process(IEnumerable<string> customFormatGuideData, RadarrConfig config)
        {
            var processedCfs = customFormatGuideData
                .Select(ProcessedCustomFormatData.CreateFromJson)
                .ToList();

            // For each ID listed under the `trash_ids` YML property, match it to an existing CF
            CustomFormats.AddRange(config.CustomFormats
                .Select(c => c.TrashId)
                .Distinct()
                .Join(processedCfs,
                    id => id,
                    cf => cf.TrashId,
                    (_, cf) => cf,
                    StringComparer.InvariantCultureIgnoreCase));
        }
    }
}
