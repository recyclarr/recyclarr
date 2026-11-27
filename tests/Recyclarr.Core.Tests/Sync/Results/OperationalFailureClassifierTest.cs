using Recyclarr.Compatibility;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;

namespace Recyclarr.Core.Tests.Sync.Results;

// Refit exception kinds are classified through InstanceSyncProcessorTest, which builds real
// Refit exceptions against a loopback listener.
internal sealed class OperationalFailureClassifierTest
{
    public static IEnumerable<TestDataRow<Func<(Exception, Type)>>> ExpectedFailures()
    {
        yield return new(
            () =>
                (
                    new ServiceIncompatibilityException("incompatible"),
                    typeof(ServiceIncompatibleFailure)
                ),
            DisplayName: "Incompatible service"
        );
        yield return new(
            () =>
                (
                    new SyncStateUnavailableException(new IOException("unavailable")),
                    typeof(SyncStateUnavailableFailure)
                ),
            DisplayName: "Sync state unavailable"
        );
        yield return new(
            () => (new HttpRequestException("unreachable"), typeof(ServiceUnavailableFailure)),
            DisplayName: "Unreachable service"
        );
    }

    [Test]
    [MethodDataSource(nameof(ExpectedFailures))]
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
