using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Processors.ErrorHandling;
using Recyclarr.Common;
using Recyclarr.TestLibrary;

namespace Recyclarr.Cli.Tests.Processors.ErrorHandling;

[TestFixture]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
public class FlurlHttpExceptionHandlerTest
{
    [Test, AutoMockData]
    public async Task Http_exception_print_validation_errors(
        [Frozen(Matching.ImplementedInterfaces)] TestableLogger log,
        IServiceErrorMessageExtractor extractor,
        FlurlHttpExceptionHandler sut
    )
    {
        var resourceReader = new ResourceDataReader(typeof(FlurlHttpExceptionHandlerTest), "Data");
        var responseContent = resourceReader.ReadData("validation_error.json");

        extractor.GetErrorMessage().Returns(responseContent);
        await sut.ProcessServiceErrorMessages(extractor);

        var logs = log.Messages.ToList();

        var expectedSubstrings = new[] { "error one", "error two" };

        logs.Should().HaveCount(expectedSubstrings.Length);
        logs.Zip(expectedSubstrings).Should().OnlyContain(pair => pair.First.Contains(pair.Second));
    }

    [Test, AutoMockData]
    public async Task Http_exception_print_plain_message(
        [Frozen(Matching.ImplementedInterfaces)] TestableLogger log,
        IServiceErrorMessageExtractor extractor,
        FlurlHttpExceptionHandler sut
    )
    {
        var resourceReader = new ResourceDataReader(typeof(FlurlHttpExceptionHandlerTest), "Data");
        var responseContent = resourceReader.ReadData("database_locked_error.json");

        extractor.GetErrorMessage().Returns(responseContent);
        await sut.ProcessServiceErrorMessages(extractor);

        var logs = log.Messages.ToList();

        var expectedSubstrings = new[] { "database is locked\ndatabase is locked" };

        logs.Should().HaveCount(expectedSubstrings.Length);
        logs.Zip(expectedSubstrings).Should().OnlyContain(pair => pair.First.Contains(pair.Second));
    }

    [Test, AutoMockData]
    public async Task Http_exception_print_title_and_errors_list(
        [Frozen(Matching.ImplementedInterfaces)] TestableLogger log,
        IServiceErrorMessageExtractor extractor,
        FlurlHttpExceptionHandler sut
    )
    {
        var resourceReader = new ResourceDataReader(typeof(FlurlHttpExceptionHandlerTest), "Data");
        var responseContent = resourceReader.ReadData("title_errors_list.json");

        extractor.GetErrorMessage().Returns(responseContent);
        await sut.ProcessServiceErrorMessages(extractor);

        var logs = log.Messages.ToList();

        var expectedSubstrings = new[]
        {
            "One or more validation errors occurred",
            "$.items[0].id: The JSON value could not be converted to System.Int32. Path: $.items[0].id | LineNumber: 0 | BytePositionInLine: 3789.",
        };

        logs.Should().HaveCount(expectedSubstrings.Length);
        logs.Zip(expectedSubstrings).Should().OnlyContain(pair => pair.First.Contains(pair.Second));
    }
}
