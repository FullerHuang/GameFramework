using GameFramework;

namespace HotUpdate
{
    public class PlayerPresenter : Presenter<IPlayerView>
    {
        readonly PlayerModel _model;
        readonly IPlayerService _service;

        public PlayerPresenter(IPlayerView view, PlayerModel model, IPlayerService service)
            : base(view)
        {
            _model = model;
            _service = service;
        }

        public override void Initialize()
        {
            AddDisposable(_model.Gold.SubscribeAndRefresh(v => View.UpdateGold(v)));
            AddDisposable(_model.Level.SubscribeAndRefresh(v => View.UpdateLevel(v)));
            AddDisposable(_model.Name.SubscribeAndRefresh(v => View.UpdateName(v)));

            View.OnLevelUpClicked += HandleLevelUp;
        }

        public override void Dispose()
        {
            View.OnLevelUpClicked -= HandleLevelUp;
            _service.SavePlayerData();
            base.Dispose();
        }

        void HandleLevelUp()
        {
            _model.LevelUp();
        }
    }
}
