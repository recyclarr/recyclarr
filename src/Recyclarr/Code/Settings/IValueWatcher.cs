using System;

namespace Recyclarr.Code.Settings
{
    public interface IValueWatcher
    {
        bool IsSame { get; }
        EventHandler<bool>? Changed { get; set; }
        void Revert();
    }
}
