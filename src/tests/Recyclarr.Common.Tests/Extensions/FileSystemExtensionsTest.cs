using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Recyclarr.Common.Extensions;
using Recyclarr.TestLibrary;

namespace Recyclarr.Common.Tests.Extensions;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class FileSystemExtensionsTest
{
    private static IEnumerable<string> ReRootFiles(
        IFileSystem fs,
        IEnumerable<string> files,
        string oldRoot,
        string newRoot)
    {
        return files.Select(x =>
        {
            var strippedPath = Regex.Replace(x, $"^{oldRoot}", newRoot);
            return fs.Path.GetFullPath(strippedPath);
        });
    }

    private static MockFileSystem NewMockFileSystem(IEnumerable<string> files, string cwd)
    {
        return NewMockFileSystem(files, Array.Empty<string>(), cwd);
    }

    private static MockFileSystem NewMockFileSystem(IEnumerable<string> files, IEnumerable<string> dirs, string cwd)
    {
        var dirData = dirs.Select(x => (x, (MockFileData) new MockDirectoryData()));
        var fileData = files.Select(x => (x, new MockFileData("")));

        return new MockFileSystem(fileData.Concat(dirData)
            .ToDictionary(x => x.Item1, y => y.Item2), FileUtils.NormalizePath(cwd));
    }

    [Test]
    public void Merge_directories_works()
    {
        var files = FileUtils.NormalizePaths(new[]
        {
            @"path1\1\file1.txt",
            @"path1\1\file2.txt",
            @"path1\1\2\3\4\file3.txt",
            @"path1\file4.txt"
        });

        var dirs = FileUtils.NormalizePaths(new[]
        {
            @"path1\empty1",
            @"path1\empty2",
            @"path1\1\2\empty3",
            @"path1\1\2\3\4\empty4"
        });

        var fs = NewMockFileSystem(files, dirs, @"C:\root\path");

        fs.MergeDirectory(
            fs.DirectoryInfo.New("path1"),
            fs.DirectoryInfo.New("path2"));

        fs.AllDirectories.Select(MockUnixSupport.Path).Should()
            .NotContain(x => x.Contains("path1") || x.Contains("empty"));

        fs.AllFiles.Should().BeEquivalentTo(ReRootFiles(fs, files, "path1", "path2"));
    }

    [Test]
    public void Fail_if_file_already_exists()
    {
        var files = FileUtils.NormalizePaths(new[]
        {
            @"path1\1\file1.txt",
            @"path1\1\file2.txt",
            @"path2\1\file1.txt"
        });

        var fs = NewMockFileSystem(files, @"C:\root\path");

        var act = () => fs.MergeDirectory(
            fs.DirectoryInfo.New("path1"),
            fs.DirectoryInfo.New("path2"));

        act.Should().Throw<IOException>();
    }

    [Test]
    public void Fail_if_directory_exists_where_file_goes()
    {
        var files = FileUtils.NormalizePaths(new[]
        {
            @"path1\1\file1"
        });

        var dirs = FileUtils.NormalizePaths(new[]
        {
            @"path2\1\file1"
        });

        var fs = NewMockFileSystem(files, dirs, @"C:\root\path");

        var act = () => fs.MergeDirectory(
            fs.DirectoryInfo.New("path1"),
            fs.DirectoryInfo.New("path2"));

        act.Should().Throw<IOException>();
    }

    [Test]
    public void Return_null_when_no_yaml_files_exist()
    {
        var fs = new MockFileSystem();
        var result = fs.CurrentDirectory().YamlFile("test");
        result.Should().BeNull();
    }

    [TestCase("test.yml")]
    [TestCase("test.yaml")]
    public void Return_non_null_when_single_yaml_file_exists(string yamlFilename)
    {
        var fs = new MockFileSystem();
        fs.AddEmptyFile(yamlFilename);

        var result = fs.CurrentDirectory().YamlFile("test");
        result.Should().NotBeNull();
        result!.Name.Should().Be(yamlFilename);
    }

    [Test]
    public void Throw_when_both_files_exist()
    {
        var fs = new MockFileSystem();
        fs.AddEmptyFile("test.yml");
        fs.AddEmptyFile("test.yaml");

        var act = () => fs.CurrentDirectory().YamlFile("test");
        act.Should().Throw<ConflictingYamlFilesException>();
    }
}
