namespace Recyclarr.Pipelines;

internal interface IPreviewRenderer<in T>
{
    void Render(string description, string instanceName, T data);
}
