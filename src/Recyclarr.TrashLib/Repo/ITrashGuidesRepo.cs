using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Repo;

public interface ITrashGuidesRepo
{
    IDirectoryInfo Path { get; }
    Task Update();
}
