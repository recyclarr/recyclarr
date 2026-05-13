using Recyclarr.Pipelines;
using Spectre.Console;

namespace Recyclarr.Cli.Preview;

internal abstract class PreviewRenderer<T>(IAnsiConsole console) : IPreviewRenderer<T>
{
    protected IAnsiConsole Console => console;

    public void Render(string description, string instanceName, T data)
    {
        Console.WriteLine();
        Console.MarkupLine(
            $"── [bold]{description}[/] [red](Preview)[/] [dim][[{instanceName}]][/] ──"
        );
        RenderData(data);
    }

    protected abstract void RenderData(T data);
}
