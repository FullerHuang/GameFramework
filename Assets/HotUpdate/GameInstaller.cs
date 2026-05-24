using VContainer;
using GameFramework;

namespace HotUpdate
{
    public class GameInstaller : IHotUpdateInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IPlayerService, PlayerService>(Lifetime.Singleton);
        }
    }
}
