using System;
using Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Menu
{
    public sealed class MenuView : UIView
    {
        [SerializeField] private Button _playButton;

        public event Action PlayRequested;

        private void OnEnable()
        {
            if (_playButton)
                _playButton.onClick.AddListener(NotifyPlayRequested);
        }

        private void OnDisable()
        {
            if (_playButton)
                _playButton.onClick.RemoveListener(NotifyPlayRequested);
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            if (_playButton)
                _playButton.interactable = isEnabled;
        }

        private void NotifyPlayRequested()
        {
            PlayRequested?.Invoke();
        }
    }
}
