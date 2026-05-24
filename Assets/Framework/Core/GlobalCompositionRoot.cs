using VContainer;
using VContainer.Unity;

namespace GameFramework
{
    public class GlobalCompositionRoot : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var appBuilder = new AppBuilder();

            appBuilder.Register(b => b.Register<IInputService, InputService>(Lifetime.Singleton));
            appBuilder.Register(b => b.Register<IAudioService, AudioService>(Lifetime.Singleton));
            appBuilder.Register(b => b.Register<ISceneService, SceneService>(Lifetime.Singleton));

            appBuilder.RegisterHotUpdate();
            appBuilder.Build(builder);
        }
    }
}
