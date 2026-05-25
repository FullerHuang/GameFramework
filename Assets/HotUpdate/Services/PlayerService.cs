using System.Collections.Generic;
using GameFramework;

namespace HotUpdate
{
    public interface IPlayerService : IService
    {
        /// <summary>工厂方法：创建新玩家</summary>
        PlayerModel CreatePlayer(string id, string name, int initialGold = 0);

        /// <summary>获取指定玩家，不存在返回 null</summary>
        PlayerModel GetPlayer(string id);

        /// <summary>尝试获取玩家</summary>
        bool TryGetPlayer(string id, out PlayerModel model);

        /// <summary>获取或创建玩家：存在则返回，不存在则自动创建</summary>
        PlayerModel GetOrCreate(string id);

        /// <summary>移除玩家</summary>
        void RemovePlayer(string id);

        /// <summary>所有玩家列表</summary>
        IReadOnlyList<PlayerModel> AllPlayers { get; }

        /// <summary>保存所有玩家数据</summary>
        void SaveAllPlayers();
    }

    public class PlayerService : IPlayerService
    {
        readonly Dictionary<string, PlayerModel> _players = new();

        public IReadOnlyList<PlayerModel> AllPlayers
        {
            get
            {
                var list = new List<PlayerModel>(_players.Values);
                return list.AsReadOnly();
            }
        }

        public PlayerModel CreatePlayer(string id, string name, int initialGold = 0)
        {
            if (_players.ContainsKey(id))
                return _players[id];

            var model = new PlayerModel(id);
            model.SetName(name);
            model.AddGold(initialGold);
            _players[id] = model;
            return model;
        }

        public PlayerModel GetPlayer(string id)
        {
            return _players.TryGetValue(id, out var model) ? model : null;
        }

        public bool TryGetPlayer(string id, out PlayerModel model)
        {
            return _players.TryGetValue(id, out model);
        }

        public PlayerModel GetOrCreate(string id)
        {
            if (_players.TryGetValue(id, out var model))
                return model;

            var newModel = new PlayerModel(id);
            newModel.SetName(id);
            _players[id] = newModel;
            return newModel;
        }

        public void RemovePlayer(string id)
        {
            if (_players.TryGetValue(id, out var model))
            {
                model.Dispose();
                _players.Remove(id);
            }
        }

        public void SaveAllPlayers()
        {
            foreach (var kv in _players)
            {
                var m = kv.Value;
                UnityEngine.Debug.Log($"[PlayerService] Saved: {m.Id} | {m.Name.Value} | Gold={m.Gold.Value} | Lv={m.Level.Value}");
            }
        }
    }
}
