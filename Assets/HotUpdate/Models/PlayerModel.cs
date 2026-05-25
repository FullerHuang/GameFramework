using GameFramework;

namespace HotUpdate
{
    public class PlayerModel : Model
    {
        public string Id { get; }

        public ReactiveProperty<int> Gold { get; } = new(0);
        public ReactiveProperty<int> Level { get; } = new(1);
        public ReactiveProperty<string> Name { get; } = new("Player");

        public PlayerModel(string id)
        {
            Id = id;
        }

        public void AddGold(int amount)
        {
            Gold.Value += amount;
        }

        public void LevelUp()
        {
            Level.Value++;
        }

        public void SetName(string name)
        {
            Name.Value = name;
        }
    }
}
