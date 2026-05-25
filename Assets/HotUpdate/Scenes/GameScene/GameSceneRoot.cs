using VContainer;
using VContainer.Unity;
using GameFramework;

namespace HotUpdate
{
    public class GameSceneRoot : LifetimeScope
    {
        protected override LifetimeScope FindParent()
        {
            return LifetimeScope.Find<GlobalCompositionRoot>();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PlayerPresenter>(Lifetime.Transient);
        }
    }
}

