using System;
using Runtime.Domain.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runtime.UI.Menu.Components
{
    public sealed class LevelCardView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private RectTransform _pressTarget;
        [SerializeField, Min(0f)] private float _pressDepth = 24f;
        [SerializeField, Min(0f)] private float _pressSpeed = 600f;

        private LevelDefinition _level;
        private Action<LevelDefinition> _clickHandler;
        private Vector2 _releasedPosition;
        private Vector2 _targetPosition;

        private void Awake()
        {
            _releasedPosition = _pressTarget.anchoredPosition;
            _targetPosition = _releasedPosition;
            _button.onClick.AddListener(HandleClick);
        }

        private void Update()
        {
            _pressTarget.anchoredPosition = Vector2.MoveTowards(
                _pressTarget.anchoredPosition,
                _targetPosition,
                _pressSpeed * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            Release(immediate: true);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.IsInteractable())
                return;

            _targetPosition = _releasedPosition + Vector2.down * _pressDepth;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
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

        private void HandleClick()
        {
            if (_level != null)
                _clickHandler?.Invoke(_level);
        }

        private void Release(bool immediate = false)
        {
            _targetPosition = _releasedPosition;

            if (immediate)
                _pressTarget.anchoredPosition = _releasedPosition;
        }
    }
}
