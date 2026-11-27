using Spectre.Console;

namespace Recyclarr.Core.TestLibrary;

public static class TestAnsiConsole
{
    public static IAnsiConsole Create()
    {
        return AnsiConsole.Create(
            new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new TestAnsiConsoleOutput(),
                Interactive = InteractionSupport.No,
                ExclusivityMode = new NoopExclusivityMode(),
                Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false },
            }
        );
    }

    private sealed class NoopExclusivityMode : IExclusivityMode
    {
        public T Run<T>(Func<T> func) => func();

        public Task<T> RunAsync<T>(Func<Task<T>> func) => func();
    }
}
