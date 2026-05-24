using VContainer;
using VContainer.Unity;

namespace HotUpdate
{
    public class GameSceneRoot : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<PlayerPresenter>(Lifetime.Transient);
            builder.Register<PlayerModel>(resolver =>
            {
                var service = resolver.Resolve<IPlayerService>();
                return service.GetPlayerModel();
            }, Lifetime.Scoped);
        }
    }
}
