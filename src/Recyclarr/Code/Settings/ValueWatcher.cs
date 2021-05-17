using System;
using System.Collections.Generic;

namespace Recyclarr.Code.Settings
{
    internal class ValueWatcher<T> : IValueWatcher
        where T : class
    {
        private readonly Comparer<T> _comparer = Comparer<T>.Default;
        private readonly T _originalValue;
        private readonly Action<T> _setter;
        private readonly Func<T> _value;

        public ValueWatcher(Func<T> value, Action<T> setter, Comparer<T>? comparer = null)
        {
            _originalValue = value();
            _value = value;
            _setter = setter;

            if (comparer != null)
            {
                _comparer = comparer;
            }
        }

        public T Value
        {
            get => _value();
            set
            {
                var wasSame = IsSame;
                _setter(value);
                if (wasSame != IsSame)
                {
                    OnChanged();
                }
            }
        }

        public bool IsSame => _comparer.Compare(_originalValue, _value()) == 0;

        public void Revert()
        {
            Value = _originalValue;
        }

        public EventHandler<bool>? Changed { get; set; }

        private void OnChanged()
        {
            Changed?.Invoke(this, IsSame);
        }
    }
}
