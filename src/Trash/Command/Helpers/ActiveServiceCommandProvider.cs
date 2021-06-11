using System;

namespace Trash.Command.Helpers
{
    public class ActiveServiceCommandProvider : IActiveServiceCommandProvider
    {
        private IServiceCommand? _activeCommand;

        public IServiceCommand ActiveCommand
        {
            get => _activeCommand ??
                   throw new InvalidOperationException("The active command has not yet been determined");
            set => _activeCommand = value;
        }
    }
}
