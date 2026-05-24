using System;

namespace GameFramework
{
    public abstract class Model : IDisposable
    {
        protected readonly DisposeBag DisposeBag = new();

        public virtual void Dispose()
        {
            DisposeBag.Dispose();
        }
    }
}
