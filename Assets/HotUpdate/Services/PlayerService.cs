using GameFramework;

namespace HotUpdate
{
    public interface IPlayerService : IService
    {
        PlayerModel GetPlayerModel();
        void SavePlayerData();
    }

    public class PlayerService : IPlayerService
    {
        PlayerModel _cachedModel;

        public PlayerModel GetPlayerModel()
        {
            if (_cachedModel == null)
            {
                _cachedModel = new PlayerModel();
                _cachedModel.SetName("Hero");
                _cachedModel.AddGold(100);
            }
            return _cachedModel;
        }

        public void SavePlayerData()
        {
            UnityEngine.Debug.Log($"[PlayerService] Saved: {_cachedModel.Name.Value}, Gold={_cachedModel.Gold.Value}");
        }
    }
}
