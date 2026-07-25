using System;
using Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Game
{
    public sealed class GameView : UIView
    {
        [SerializeField] private Button _menuButton;

        public event Action MenuRequested;

        private void OnEnable()
        {
            if (_menuButton)
                _menuButton.onClick.AddListener(NotifyMenuRequested);
        }

        private void OnDisable()
        {
            if (_menuButton)
                _menuButton.onClick.RemoveListener(NotifyMenuRequested);
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            if (_menuButton)
                _menuButton.interactable = isEnabled;
        }

        private void NotifyMenuRequested()
        {
            MenuRequested?.Invoke();
        }
    }
}
