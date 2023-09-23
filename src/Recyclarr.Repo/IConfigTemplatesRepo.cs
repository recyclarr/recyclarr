using System.IO.Abstractions;

namespace Recyclarr.Repo;

public interface IConfigTemplatesRepo
{
    IDirectoryInfo Path { get; }
}
