using FluentAssertions;
using NUnit.Framework;

namespace Common.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ResourceDataReaderTest
{
    [Test]
    public void GetResourceData_DefaultDir_ReturnResourceData()
    {
        var testData = new ResourceDataReader(typeof(ResourceDataReaderTest));
        var data = testData.ReadData("DefaultDataFile.txt");
        data.Trim().Should().Be("DefaultDataFile");
    }

    [Test]
    public void GetResourceData_NonexistentFile_Throw()
    {
        var testData = new ResourceDataReader(typeof(ResourceDataReaderTest));
        Action act = () => testData.ReadData("DataFileWontBeFound.txt");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Embedded resource not found*");
    }

    [Test]
    public void ReadData_ExplicitSubDir_ReturnResourceData()
    {
        var testData = new ResourceDataReader(typeof(ResourceDataReaderTest), "TestData");
        var data = testData.ReadData("DataFile.txt");
        data.Trim().Should().Be("DataFile");
    }
}
