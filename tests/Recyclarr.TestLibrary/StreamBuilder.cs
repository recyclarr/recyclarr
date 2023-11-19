using System.Text;

namespace Recyclarr.TestLibrary;

public static class StreamBuilder
{
    public static StreamReader FromString(string data)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
        return new StreamReader(stream);
    }
}
