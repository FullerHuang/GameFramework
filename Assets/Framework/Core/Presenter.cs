using System;

namespace GameFramework
{
    public abstract class Presenter<TView> : IDisposable where TView : IView
    {
        protected readonly TView View;
        readonly DisposeBag _disposeBag = new();

        protected Presenter(TView view)
        {
            View = view;
        }

        public virtual void Initialize() { }

        protected void AddDisposable(IDisposable disposable) => _disposeBag.Add(disposable);
        protected void AddDisposable(Action onDispose) => _disposeBag.Add(onDispose);

        public virtual void Dispose()
        {
            _disposeBag.Dispose();
        }
    }
}
