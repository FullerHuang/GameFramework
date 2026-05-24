using System;
using GameFramework;

namespace HotUpdate
{
    public interface IPlayerView : IView
    {
        void UpdateGold(int gold);
        void UpdateLevel(int level);
        void UpdateName(string name);
        event Action OnLevelUpClicked;
    }
}
