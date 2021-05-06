using System.Collections.Generic;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;

namespace Trash.Radarr.CustomFormat
{
    public interface ICachePersister
    {
        CustomFormatCache? CfCache { get; }
        void Load();
        void Save();
        void Update(IEnumerable<ProcessedCustomFormatData> customFormats);
    }
}
