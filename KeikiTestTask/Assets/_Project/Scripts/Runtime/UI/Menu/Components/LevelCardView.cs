using System;
using Runtime.Domain.Levels;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Menu.Components
{
    public sealed class LevelCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;

        private LevelDefinition _level;
        private Action<LevelDefinition> _clickHandler;

        private void Awake()
        {
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(
            LevelDefinition level,
            Sprite icon,
            Action<LevelDefinition> clickHandler)
        {
            _level = level
                ?? throw new ArgumentNullException(nameof(level));
            _clickHandler = clickHandler
                ?? throw new ArgumentNullException(nameof(clickHandler));

            if (!icon)
                throw new ArgumentNullException(nameof(icon));

            if (!ColorUtility.TryParseHtmlString(level.ColorHex, out Color color))
            {
                throw new ArgumentException(
                    $"Level '{level.Id}' contains invalid color '{level.ColorHex}'.",
                    nameof(level));
            }

            name = $"Level - {level.Id}";
            _icon.sprite = icon;
            _icon.color = color;
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            _button.interactable = isEnabled;
        }

        private void HandleClick()
        {
            if (_level != null)
                _clickHandler?.Invoke(_level);
        }
    }
}
