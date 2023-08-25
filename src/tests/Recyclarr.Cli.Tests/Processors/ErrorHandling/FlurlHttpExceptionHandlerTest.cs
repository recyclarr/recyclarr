using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cli.Processors.ErrorHandling;
using Recyclarr.Common;
using Recyclarr.TestLibrary;

namespace Recyclarr.Cli.Tests.Processors.ErrorHandling;

[TestFixture]
[Parallelizable(ParallelScope.All)]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
public class FlurlHttpExceptionHandlerTest
{
    [Test, AutoMockData]
    public async Task Http_exception_print_validation_errors(
        [Frozen(Matching.ImplementedInterfaces)] TestableLogger log,
        IServiceErrorMessageExtractor extractor,
        FlurlHttpExceptionHandler sut)
    {
        var resourceReader = new ResourceDataReader(typeof(FlurlHttpExceptionHandlerTest), "Data");
        var responseContent = resourceReader.ReadData("validation_error.json");

        extractor.GetErrorMessage().Returns(responseContent);
        await sut.ProcessServiceErrorMessages(extractor);

        var logs = log.Messages;

        logs.Should().BeEquivalentTo(new[]
            {
                "Reason: error one",
                "Reason: error two"
            },
            o => o.WithStrictOrdering()
        );
    }

    [Test, AutoMockData]
    public async Task Http_exception_print_plain_message(
        [Frozen(Matching.ImplementedInterfaces)] TestableLogger log,
        IServiceErrorMessageExtractor extractor,
        FlurlHttpExceptionHandler sut)
    {
        var resourceReader = new ResourceDataReader(typeof(FlurlHttpExceptionHandlerTest), "Data");
        var responseContent = resourceReader.ReadData("database_locked_error.json");

        extractor.GetErrorMessage().Returns(responseContent);
        await sut.ProcessServiceErrorMessages(extractor);

        var logs = log.Messages;

        logs.Should().BeEquivalentTo(new[]
            {
                "Reason: database is locked\ndatabase is locked"
            },
            o => o.WithStrictOrdering()
        );
    }
}
