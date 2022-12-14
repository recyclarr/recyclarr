using System.IO.Abstractions.TestingHelpers;
using Newtonsoft.Json;

namespace Recyclarr.TestLibrary;

public static class MockData
{
    public static MockFileData FromJson(object json)
    {
        return new MockFileData(JsonConvert.SerializeObject(json));
    }

    public static MockFileData FromString(string data)
    {
        return new MockFileData(data);
    }
}
