using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Common.FluentValidation;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification =
    "Used by AutoFac to exclude IValidator implementations from DI registration")]
public interface IManualValidator
{
}
