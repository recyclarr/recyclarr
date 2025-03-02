using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;

namespace Recyclarr.TestLibrary;

public static class MockData
{
    public static MockFileData FromJson(object json, JsonSerializerOptions options)
    {
        return new MockFileData(JsonSerializer.Serialize(json, options));
    }

    public static MockFileData FromString(string data)
    {
        return new MockFileData(data);
    }
}
