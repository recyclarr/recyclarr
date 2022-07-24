using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using CliFx.Infrastructure;

namespace Common;

/// <summary>
///     An ASCII progress bar
/// </summary>
public sealed class ProgressBar //: IProgress<double>
{
    private readonly IConsole _console;
    private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
    private const string Animation = @"|/-\";
    private int _animationIndex;

    private readonly Subject<float> _reportProgress = new();
    public IObserver<float> ReportProgress => _reportProgress;
    public string Description { get; set; } = "";

    public ProgressBar(IConsole console)
    {
        _console = console;

        // A progress bar is only for temporary display in a console window.
        // If the console output is redirected to a file, draw nothing.
        // Otherwise, we'll end up with a lot of garbage in the target file.
        if (!_console.IsOutputRedirected)
        {
            _reportProgress.Sample(_animationInterval)
                .Select(CalculateText)
                .StartWith(string.Empty)
                .Buffer(2, 1) // sliding window: take previous and current
                .Subscribe(x => UpdateText(x[0].Length, x[1]));
        }
    }

    private string CalculateText(float progress)
    {
        const int blockCount = 10;
        var progressBlockCount = (int) (progress * blockCount);
        var percent = (int) (progress * 100);
        var progressBlocks = new string('#', progressBlockCount);
        var progressBlocksUnfilled = new string('-', blockCount - progressBlockCount);
        var currentAnimationFrame = Animation[_animationIndex++ % Animation.Length];
        return $"[{progressBlocks}{progressBlocksUnfilled}] {percent,3}% {currentAnimationFrame} {Description}";
    }

    private void UpdateText(int previousTextLength, string text)
    {
        var outputBuilder = new StringBuilder();
        outputBuilder.Append('\r');
        outputBuilder.Append(text);

        // If the previous string was longer, "erase" the old characters with spaces.
        var lengthDifference = previousTextLength - text.Length;
        if (lengthDifference > 0)
        {
            outputBuilder.Append(' ', lengthDifference);
        }

        _console.Output.Write(outputBuilder);
    }
}
