using System;
using System.Collections.Generic;

namespace GameFramework
{
    public sealed class DisposeBag : IDisposable
    {
        readonly List<IDisposable> _disposables = new();
        bool _disposed;

        public void Add(IDisposable disposable)
        {
            if (!_disposed) _disposables.Add(disposable);
        }

        public void Add(Action onDispose)
        {
            if (!_disposed) _disposables.Add(new ActionDisposable(onDispose));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            for (int i = _disposables.Count - 1; i >= 0; i--)
                _disposables[i]?.Dispose();
            _disposables.Clear();
        }

        sealed class ActionDisposable : IDisposable
        {
            Action _action;
            public ActionDisposable(Action action) => _action = action;
            public void Dispose() => _action?.Invoke();
        }
    }
}
