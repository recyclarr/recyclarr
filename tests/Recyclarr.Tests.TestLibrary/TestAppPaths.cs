using System.IO.Abstractions;
using Recyclarr.Platform;

namespace Recyclarr.Tests.TestLibrary;

public sealed class TestAppPaths(IFileSystem fs) : AppPaths(fs.CurrentDirectory().SubDirectory("recyclarr"));
