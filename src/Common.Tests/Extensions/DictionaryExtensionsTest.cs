using System.Collections.Generic;
using Common.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace Common.Tests.Extensions
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class DictionaryExtensionsTest
    {
        private class MySampleValue
        {
        }

        [Test]
        public void GetOrCreate_ItemExists_ReturnExistingItem()
        {
            var sample = new MySampleValue();
            var dict = new Dictionary<int, MySampleValue> {{100, sample}};

            var theValue = dict.GetOrCreate(100);
            dict.Should().HaveCount(1);
            dict.Should().Contain(100, sample);
            dict.Should().ContainValue(theValue);
            theValue.Should().Be(sample);
        }

        [Test]
        public void GetOrCreate_NoItemExists_ItIsCreated()
        {
            var dict = new Dictionary<int, MySampleValue>();
            var theValue = dict.GetOrCreate(100);
            dict.Should().HaveCount(1);
            dict.Should().Contain(100, theValue);
        }

        [Test]
        public void GetOrDefault_ItemExists_ReturnExistingItem()
        {
        }
    }
}
