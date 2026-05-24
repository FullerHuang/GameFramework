using System;
using System.Collections.Generic;
using VContainer;
using VContainer.Unity;

namespace GameFramework
{
    public class AppBuilder
    {
        readonly List<Action<IContainerBuilder>> _registrations = new();

        public AppBuilder Register(Action<IContainerBuilder> registration)
        {
            _registrations.Add(registration);
            return this;
        }

        public AppBuilder RegisterHotUpdate()
        {
            var type = Type.GetType("HotUpdate.GameInstaller, HotUpdate");
            if (type != null && typeof(IHotUpdateInstaller).IsAssignableFrom(type))
            {
                var installer = (IHotUpdateInstaller)Activator.CreateInstance(type);
                _registrations.Add(builder => installer.Install(builder));
            }
            return this;
        }

        public void Build(IContainerBuilder builder)
        {
            foreach (var reg in _registrations)
                reg(builder);
        }
    }
}
