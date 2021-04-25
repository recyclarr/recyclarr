using System;
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using Trash.Cache;

namespace Trash.Tests.Cache
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ServiceCacheTest
    {
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
        public void Load_NoFileExists_ThrowsException()
        {
            // use a real filesystem to test no file existing
            var filesystem = new FileSystem();
            var storagePath = Substitute.For<ICacheStoragePath>();
            var cache = new ServiceCache(filesystem, storagePath);

            Action act = () => cache.Load<ObjectWithAttribute>();

            act.Should()
                .Throw<FileNotFoundException>();
        }

        [Test]
        public void Load_WithAttribute_ParsesCorrectly()
        {
            var filesystem = Substitute.For<IFileSystem>();
            var storagePath = Substitute.For<ICacheStoragePath>();
            var cache = new ServiceCache(filesystem, storagePath);

            storagePath.Path.Returns("testpath");

            dynamic testJson = new {TestValue = "Foo"};
            filesystem.File.ReadAllText(Arg.Any<string>())
                .Returns(_ => JsonConvert.SerializeObject(testJson));

            var obj = cache.Load<ObjectWithAttribute>();

            obj.TestValue.Should().Be("Foo");
            filesystem.File.Received().ReadAllText($"testpath{Path.DirectorySeparatorChar}{ValidObjectName}.json");
        }

        [Test]
        public void Load_WithAttributeInvalidName_ThrowsException()
        {
            var filesystem = Substitute.For<IFileSystem>();
            var storagePath = Substitute.For<ICacheStoragePath>();
            var cache = new ServiceCache(filesystem, storagePath);

            Action act = () => cache.Load<ObjectWithAttributeInvalidChars>();

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("*'invalid+name' has unacceptable characters*");
        }

        [Test]
        public void Load_WithoutAttribute_Throws()
        {
            var filesystem = Substitute.For<IFileSystem>();
            var storagePath = Substitute.For<ICacheStoragePath>();
            var cache = new ServiceCache(filesystem, storagePath);

            Action act = () => cache.Load<ObjectWithoutAttribute>();

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("CacheObjectNameAttribute is missing*");
        }

        [Test]
        public void Save_WithAttribute_ParsesCorrectly()
        {
            var filesystem = Substitute.For<IFileSystem>();
            var storagePath = Substitute.For<ICacheStoragePath>();
            var cache = new ServiceCache(filesystem, storagePath);

            storagePath.Path.Returns("testpath");

            cache.Save(new ObjectWithAttribute {TestValue = "Foo"});

            dynamic expectedJson = new {TestValue = "Foo"};
            var expectedPath = $"testpath{Path.DirectorySeparatorChar}{ValidObjectName}.json";
            filesystem.File.Received()
                .WriteAllText(expectedPath, JsonConvert.SerializeObject(expectedJson, Formatting.Indented));
        }

        [Test]
        public void Save_WithAttributeInvalidName_ThrowsException()
        {
            var filesystem = Substitute.For<IFileSystem>();
            var storagePath = Substitute.For<ICacheStoragePath>();
            var cache = new ServiceCache(filesystem, storagePath);

            Action act = () => cache.Save(new ObjectWithAttributeInvalidChars());

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("*'invalid+name' has unacceptable characters*");
        }

        [Test]
        public void Save_WithoutAttribute_Throws()
        {
            var filesystem = Substitute.For<IFileSystem>();
            var storagePath = Substitute.For<ICacheStoragePath>();
            var cache = new ServiceCache(filesystem, storagePath);

            Action act = () => cache.Save(new ObjectWithoutAttribute());

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("CacheObjectNameAttribute is missing*");
        }
    }
}
