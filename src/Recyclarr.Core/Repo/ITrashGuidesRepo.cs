using System.IO.Abstractions;

namespace Recyclarr.Repo;

public interface ITrashGuidesRepo
{
    IDirectoryInfo Path { get; }
}
