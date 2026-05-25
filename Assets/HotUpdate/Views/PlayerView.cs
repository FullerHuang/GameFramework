using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;
using GameFramework;

namespace HotUpdate
{
    public class PlayerView : MonoBehaviour, IPlayerView
    {
        [SerializeField] string _playerId = "Player1";
        [SerializeField] Text _goldText;
        [SerializeField] Text _levelText;
        [SerializeField] Text _nameText;
        [SerializeField] Button _levelUpButton;

        PlayerPresenter _presenter;
        LifetimeScope _subScope;

        public event Action OnLevelUpClicked;

        void Awake()
        {
            var parentScope = GetComponentInParent<LifetimeScope>();
            _subScope = parentScope.CreateChild(builder =>
            {
                builder.RegisterInstance<IPlayerView>(this);
                builder.RegisterInstance(_playerId);
            });
            _presenter = _subScope.Container.Resolve<PlayerPresenter>();
            _presenter.Initialize();

            _levelUpButton?.onClick.AddListener(() => OnLevelUpClicked?.Invoke());
        }

        void OnDestroy()
        {
            _presenter?.Dispose();
            _subScope?.Dispose();
        }

        public void UpdateGold(int gold)
        {
            if (_goldText != null) _goldText.text = $"金币: {gold}";
        }

        public void UpdateLevel(int level)
        {
            if (_levelText != null) _levelText.text = $"等级: {level}";
        }

        public void UpdateName(string name)
        {
            if (_nameText != null) _nameText.text = name;
        }
    }
}
