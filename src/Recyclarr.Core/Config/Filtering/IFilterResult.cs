using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Config.Filtering;

[SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "This is a marker interface for filter results which are stored in a collection"
)]
public interface IFilterResult;
