using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using AutoFixture.NUnit3;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using TestLibrary.AutoFixture;
using TestLibrary.NSubstitute;
using TrashLib.Cache;
using TrashLib.Config.Services;

namespace TrashLib.Tests.Cache;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ServiceCacheTest
{
    private class Context
    {
        public Context(IFileSystem? fs = null)
        {
            Filesystem = fs ?? Substitute.For<IFileSystem>();
            StoragePath = Substitute.For<ICacheStoragePath>();
            ConfigProvider = Substitute.For<IConfigurationProvider>();

            // Set up a default for the active config's base URL. This is used to generate part of the path
            ConfigProvider.ActiveConfiguration = Substitute.For<IServiceConfiguration>();
            ConfigProvider.ActiveConfiguration.BaseUrl.Returns("http://localhost:1234");

            Cache = new ServiceCache(Filesystem, StoragePath, ConfigProvider, Substitute.For<ILogger>());
        }

        public ServiceCache Cache { get; }
        public IConfigurationProvider ConfigProvider { get; }
        public ICacheStoragePath StoragePath { get; }
        public IFileSystem Filesystem { get; }
    }

    private class ObjectWithoutAttribute
    {
    }

    private const string ValidObjectName = "azAZ_09";

    [CacheObjectName(ValidObjectName)]
    private class ObjectWithAttribute
    {
        public string TestValue { get; init; } = "";
    }

    [CacheObjectName("invalid+name")]
    private class ObjectWithAttributeInvalidChars
    {
    }

    [Test]
    public void Load_returns_null_when_file_does_not_exist()
    {
        var ctx = new Context();
        ctx.Filesystem.File.Exists(Arg.Any<string>()).Returns(false);

        var result = ctx.Cache.Load<ObjectWithAttribute>();
        result.Should().BeNull();
    }

    [Test, AutoMockData]
    public void Loading_with_attribute_parses_correctly(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IServiceConfiguration config,
        [Frozen] ICacheStoragePath storage,
        ServiceCache sut)
    {
        const string testJson = @"{'test_value': 'Foo'}";

        storage.Path.Returns("testpath");
        config.BaseUrl.Returns("http://localhost:1234");

        var testJsonPath = fs.CurrentDirectory()
            .SubDirectory("testpath")
            .SubDirectory("be8fbc8f")
            .File($"{ValidObjectName}.json").FullName;

        fs.AddFile(testJsonPath, new MockFileData(testJson));

        var obj = sut.Load<ObjectWithAttribute>();

        obj.Should().NotBeNull();
        obj!.TestValue.Should().Be("Foo");
    }

    [Test]
    public void Loading_with_invalid_object_name_throws()
    {
        var ctx = new Context();

        Action act = () => ctx.Cache.Load<ObjectWithAttributeInvalidChars>();

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*'invalid+name' has unacceptable characters*");
    }

    [Test]
    public void Loading_without_attribute_throws()
    {
        var ctx = new Context();

        Action act = () => ctx.Cache.Load<ObjectWithoutAttribute>();

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("CacheObjectNameAttribute is missing*");
    }

    [Test]
    public void Properties_are_saved_using_snake_case()
    {
        var ctx = new Context();
        ctx.StoragePath.Path.Returns("testpath");
        ctx.Cache.Save(new ObjectWithAttribute {TestValue = "Foo"});

        ctx.Filesystem.File.Received()
            .WriteAllText(Arg.Any<string>(), Verify.That<string>(json => json.Should().Contain("\"test_value\"")));
    }

    [Test, AutoMockData]
    public void Saving_with_attribute_parses_correctly(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IServiceConfiguration config,
        [Frozen] ICacheStoragePath storage,
        ServiceCache sut)
    {
        storage.Path.Returns("testpath");
        config.BaseUrl.Returns("http://localhost:1234");

        var testJsonPath = fs.CurrentDirectory()
            .SubDirectory("testpath")
            .SubDirectory("be8fbc8f")
            .File($"{ValidObjectName}.json").FullName;

        sut.Save(new ObjectWithAttribute {TestValue = "Foo"});

        var expectedFile = fs.GetFile(testJsonPath);
        expectedFile.Should().NotBeNull();
        expectedFile.TextContents.Should().Be(@"{
  ""test_value"": ""Foo""
}");
    }

    [Test]
    public void Saving_with_invalid_object_name_throws()
    {
        var ctx = new Context();

        var act = () => ctx.Cache.Save(new ObjectWithAttributeInvalidChars());

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*'invalid+name' has unacceptable characters*");
    }

    [Test]
    public void Saving_without_attribute_throws()
    {
        var ctx = new Context();

        var act = () => ctx.Cache.Save(new ObjectWithoutAttribute());

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("CacheObjectNameAttribute is missing*");
    }

    [Test, AutoMockData]
    public void Switching_config_and_base_url_should_yield_different_cache_paths(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IConfigurationProvider provider,
        ServiceCache sut)
    {
        provider.ActiveConfiguration.BaseUrl.Returns("http://localhost:1234");

        sut.Save(new ObjectWithAttribute {TestValue = "Foo"});

        // Change the active config & base URL so we get a different path
        provider.ActiveConfiguration.BaseUrl.Returns("http://localhost:5678");

        sut.Save(new ObjectWithAttribute {TestValue = "Bar"});

        fs.AllFiles.Should().HaveCount(2)
            .And.AllSatisfy(x => x.Should().EndWith("json"));
    }

    [Test]
    public void When_cache_file_is_empty_do_not_throw()
    {
        var ctx = new Context();
        ctx.Filesystem.File.Exists(Arg.Any<string>()).Returns(true);
        ctx.Filesystem.File.ReadAllText(Arg.Any<string>())
            .Returns(_ => "");

        Action act = () => ctx.Cache.Load<ObjectWithAttribute>();

        act.Should().NotThrow();
    }
}
