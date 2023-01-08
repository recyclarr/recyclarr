using System.ComponentModel;
using System.Globalization;
using System.IO.Abstractions;

namespace Recyclarr.Cli.Console.Helpers;

internal class FileInfoConverter : TypeConverter
{
    private readonly IFileSystem _fs;

    public FileInfoConverter(IFileSystem fs)
    {
        _fs = fs;
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        // ReSharper disable once InvertIf
        if (value is string path)
        {
            var info = _fs.FileInfo.New(path);
            if (!info.Exists)
            {
                throw new FileNotFoundException("The file does not exist", path);
            }

            return info;
        }

        return null;
    }
}
