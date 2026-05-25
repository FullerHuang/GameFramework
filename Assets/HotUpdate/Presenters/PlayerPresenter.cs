using GameFramework;

namespace HotUpdate
{
    public class PlayerPresenter : Presenter<IPlayerView>
    {
        readonly IPlayerService _service;
        readonly string _playerId;
        PlayerModel _model;

        public PlayerPresenter(IPlayerView view, string playerId, IPlayerService service)
            : base(view)
        {
            _service = service;
            _playerId = playerId;
        }

        public override void Initialize()
        {
            _model = _service.GetOrCreate(_playerId);

            AddDisposable(_model.Gold.SubscribeAndRefresh(v => View.UpdateGold(v)));
            AddDisposable(_model.Level.SubscribeAndRefresh(v => View.UpdateLevel(v)));
            AddDisposable(_model.Name.SubscribeAndRefresh(v => View.UpdateName(v)));

            View.OnLevelUpClicked += HandleLevelUp;
        }

        public override void Dispose()
        {
            View.OnLevelUpClicked -= HandleLevelUp;
            _service.SaveAllPlayers();
            base.Dispose();
        }

        void HandleLevelUp()
        {
            _model.LevelUp();
        }
    }
}
