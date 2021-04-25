using System.Threading.Tasks;
using Autofac;
using CliFx;
using Trash.Command;

namespace Trash
{
    internal static class Program
    {
        private static IContainer? _container;

        public static async Task<int> Main()
        {
            _container = CompositionRoot.Setup();
            return await new CliApplicationBuilder()
                .AddCommandsFromThisAssembly()
                .SetExecutableName(ThisAssembly.AssemblyName)
                .SetVersion($"v{ThisAssembly.AssemblyInformationalVersion}")
                .UseTypeActivator(type => CliTypeActivator.ResolveType(_container, type))
                .Build()
                .RunAsync();
        }
    }
}
