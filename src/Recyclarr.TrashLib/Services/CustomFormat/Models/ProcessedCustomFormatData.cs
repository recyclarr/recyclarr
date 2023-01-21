using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Recyclarr.TrashLib.Services.CustomFormat.Models;

public class ProcessedCustomFormatData
{
    private readonly CustomFormatData _data;

    public ProcessedCustomFormatData(CustomFormatData data)
    {
        _data = data;
        Json = _data.Json;
    }

    public string Name => _data.Name;
    public string TrashId => _data.TrashId;
    public int? Score => _data.Score;
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only")]
    public JObject Json { get; set; }
    public int FormatId { get; set; }
}
