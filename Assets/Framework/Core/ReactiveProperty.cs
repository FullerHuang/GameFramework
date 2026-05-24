using System;

namespace GameFramework
{
    public sealed class ReactiveProperty<T>
    {
        T _value;
        event Action<T> OnValueChanged;

        public ReactiveProperty() => _value = default;
        public ReactiveProperty(T initialValue) => _value = initialValue;

        public T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value)) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public IDisposable Subscribe(Action<T> callback)
        {
            OnValueChanged += callback;
            return new Subscription(this, callback);
        }

        public IDisposable SubscribeAndRefresh(Action<T> callback)
        {
            callback(_value);
            return Subscribe(callback);
        }

        public void Unsubscribe(Action<T> callback)
        {
            OnValueChanged -= callback;
        }

        sealed class Subscription : IDisposable
        {
            readonly ReactiveProperty<T> _owner;
            readonly Action<T> _callback;

            public Subscription(ReactiveProperty<T> owner, Action<T> callback)
            {
                _owner = owner;
                _callback = callback;
            }

            public void Dispose()
            {
                _owner.Unsubscribe(_callback);
            }
        }
    }
}
