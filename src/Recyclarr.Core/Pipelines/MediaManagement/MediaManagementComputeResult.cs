using Recyclarr.Servarr.MediaManagement;

namespace Recyclarr.Pipelines.MediaManagement;

internal record MediaManagementComputeResult(
    MediaManagementData Current,
    MediaManagementData Desired
);
