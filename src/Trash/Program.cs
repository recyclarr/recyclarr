using System.Threading.Tasks;
using Autofac;
using CliFx;

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
                .SetVersion(ThisAssembly.AssemblyInformationalVersion)
                .UseTypeActivator(type => _container.Resolve(type))
                .Build()
                .RunAsync();
        }
    }
}
