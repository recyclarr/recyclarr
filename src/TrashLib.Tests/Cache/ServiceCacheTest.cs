using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using TestLibrary.NSubstitute;
using TrashLib.Cache;
using TrashLib.Config;

namespace TrashLib.Tests.Cache
{
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
                JsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                };

                Cache = new ServiceCache(Filesystem, StoragePath, Substitute.For<ILogger>());
            }

            public JsonSerializerSettings JsonSettings { get; }
            public ServiceCache Cache { get; }
            public ICacheStoragePath StoragePath { get; }
            public IFileSystem Filesystem { get; }

            public ICacheGuidBuilder MakeGuidBuilder(string baseUrl = "http://localhost:1234")
            {
                return new CacheGuidBuilder(new TestServiceConfig
                    {BaseUrl = baseUrl});
            }

            private class TestServiceConfig : ServiceConfiguration
            {
            }
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

            var result = ctx.Cache.Load<ObjectWithAttribute>(ctx.MakeGuidBuilder());
            result.Should().BeNull();
        }

        [Test]
        public void Loading_with_attribute_parses_correctly()
        {
            var ctx = new Context();

            ctx.StoragePath.Path.Returns("testpath");

            dynamic testJson = new {TestValue = "Foo"};
            ctx.Filesystem.File.Exists(Arg.Any<string>()).Returns(true);
            ctx.Filesystem.File.ReadAllText(Arg.Any<string>())
                .Returns(_ => JsonConvert.SerializeObject(testJson));

            var obj = ctx.Cache.Load<ObjectWithAttribute>(ctx.MakeGuidBuilder());

            obj.Should().NotBeNull();
            obj!.TestValue.Should().Be("Foo");
            ctx.Filesystem.File.Received().ReadAllText(Path.Combine("testpath", "be8fbc8f", $"{ValidObjectName}.json"));
        }

        [Test]
        public void Loading_with_invalid_object_name_throws()
        {
            var ctx = new Context();

            Action act = () => ctx.Cache.Load<ObjectWithAttributeInvalidChars>(ctx.MakeGuidBuilder());

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("*'invalid+name' has unacceptable characters*");
        }

        [Test]
        public void Loading_without_attribute_throws()
        {
            var ctx = new Context();

            Action act = () => ctx.Cache.Load<ObjectWithoutAttribute>(ctx.MakeGuidBuilder());

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("CacheObjectNameAttribute is missing*");
        }

        [Test]
        public void Properties_are_saved_using_snake_case()
        {
            var ctx = new Context();
            ctx.StoragePath.Path.Returns("testpath");
            ctx.Cache.Save(new ObjectWithAttribute {TestValue = "Foo"}, ctx.MakeGuidBuilder());

            ctx.Filesystem.File.Received()
                .WriteAllText(Arg.Any<string>(), Verify.That<string>(json => json.Should().Contain("\"test_value\"")));
        }

        [Test]
        public void Saving_with_attribute_parses_correctly()
        {
            var ctx = new Context();

            ctx.StoragePath.Path.Returns("testpath");

            ctx.Cache.Save(new ObjectWithAttribute {TestValue = "Foo"}, ctx.MakeGuidBuilder());

            var expectedParentDirectory = Path.Combine("testpath", "be8fbc8f");
            ctx.Filesystem.Directory.Received().CreateDirectory(expectedParentDirectory);

            dynamic expectedJson = new {TestValue = "Foo"};
            var expectedPath = Path.Combine(expectedParentDirectory, $"{ValidObjectName}.json");
            ctx.Filesystem.File.Received()
                .WriteAllText(expectedPath, JsonConvert.SerializeObject(expectedJson, ctx.JsonSettings));
        }

        [Test]
        public void Saving_with_invalid_object_name_throws()
        {
            var ctx = new Context();

            Action act = () => ctx.Cache.Save(new ObjectWithAttributeInvalidChars(), ctx.MakeGuidBuilder());

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("*'invalid+name' has unacceptable characters*");
        }

        [Test]
        public void Saving_without_attribute_throws()
        {
            var ctx = new Context();

            Action act = () => ctx.Cache.Save(new ObjectWithoutAttribute(), ctx.MakeGuidBuilder());

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("CacheObjectNameAttribute is missing*");
        }

        [Test]
        public void Switching_config_and_base_url_should_yield_different_cache_paths()
        {
            var ctx = new Context();
            ctx.StoragePath.Path.Returns("testpath");

            var actualPaths = new List<string>();

            dynamic testJson = new {TestValue = "Foo"};
            ctx.Filesystem.File.Exists(Arg.Any<string>()).Returns(true);
            ctx.Filesystem.File.ReadAllText(Arg.Do<string>(s => actualPaths.Add(s)))
                .Returns(_ => JsonConvert.SerializeObject(testJson));

            ctx.Cache.Load<ObjectWithAttribute>(ctx.MakeGuidBuilder());

            // Change the active config & base URL so we get a different path
            ctx.Cache.Load<ObjectWithAttribute>(ctx.MakeGuidBuilder("http://localhost:5678"));

            actualPaths.Count.Should().Be(2);
            actualPaths.Should().OnlyHaveUniqueItems();
        }

        [Test]
        public void When_cache_file_is_empty_do_not_throw()
        {
            var ctx = new Context();
            ctx.Filesystem.File.Exists(Arg.Any<string>()).Returns(true);
            ctx.Filesystem.File.ReadAllText(Arg.Any<string>())
                .Returns(_ => "");

            Action act = () => ctx.Cache.Load<ObjectWithAttribute>(ctx.MakeGuidBuilder());

            act.Should().NotThrow();
        }
    }
}
