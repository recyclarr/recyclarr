using Recyclarr.Compatibility;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.Sync.Results;

// Refit exception kinds are classified through InstanceSyncProcessorTest, which builds real
// Refit exceptions against a loopback listener.
internal sealed class OperationalFailureClassifierTest
{
    private static IEnumerable<TestCaseData> ExpectedFailures()
    {
        yield return new TestCaseData(
            new ServiceIncompatibilityException("incompatible"),
            typeof(ServiceIncompatibleFailure)
        ).SetName("Incompatible service");
        yield return new TestCaseData(
            new SyncStateUnavailableException(new IOException("unavailable")),
            typeof(SyncStateUnavailableFailure)
        ).SetName("Sync state unavailable");
        yield return new TestCaseData(
            new HttpRequestException("unreachable"),
            typeof(ServiceUnavailableFailure)
        ).SetName("Unreachable service");
    }

    [TestCaseSource(nameof(ExpectedFailures))]
    public void Expected_failure_is_classified(Exception exception, Type expectedType)
    {
        OperationalFailureClassifier.Classify(exception).Should().BeOfType(expectedType);
    }

    [Test]
    public void Unexpected_exception_is_not_classified()
    {
        OperationalFailureClassifier
            .Classify(new InvalidOperationException("unexpected"))
            .Should()
            .BeNull();
    }
}
