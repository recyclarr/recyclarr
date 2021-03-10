using System;
using FluentAssertions;
using NUnit.Framework;

namespace TestLibrary.Tests
{
    internal class TestFixtureMissingAttribute
    {
    }

    [TestFixture]
    public class TestDataTest
    {
        [Test]
        public void Construction_ClassMissingAttribute_Throw()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action act = () => new TestData<TestFixtureMissingAttribute>();

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("*does not have the [TestFixture] attribute");
        }

        [Test]
        public void GetResourceData_CustomDir_ReturnResourceData()
        {
            TestData<TestDataTest> testData = new();
            testData.DataSubdirectoryName = "OtherData";
            var data = testData.GetResourceData("AnotherDataFile.txt");
            data.Trim().Should().Be("AnotherDataFile");
        }

        [Test]
        public void GetResourceData_DefaultDir_ReturnResourceData()
        {
            TestData<TestDataTest> testData = new();
            var data = testData.GetResourceData("DataFile.txt");
            data.Trim().Should().Be("DataFile");
        }

        [Test]
        public void GetResourceData_NonexistentFile_Throw()
        {
            TestData<TestDataTest> testData = new();
            Action act = () => testData.GetResourceData("DataFileWontBeFound.txt");

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("Embedded resource not found*");
        }
    }
}
