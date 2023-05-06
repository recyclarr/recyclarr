using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Repo;

public interface IConfigTemplatesRepo
{
    IDirectoryInfo Path { get; }
    Task Update();
}
