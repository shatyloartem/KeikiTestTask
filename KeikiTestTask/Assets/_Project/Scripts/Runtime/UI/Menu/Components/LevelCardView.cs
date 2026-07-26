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
        IInitializePotentialDragHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private RectTransform _pressTarget;
        [SerializeField, Min(0f)] private float _pressDepth = 24f;

        private LevelDefinition _level;
        private Action<LevelDefinition> _clickHandler;
        private ScrollRect _scrollRect;
        private RectTransform _rectTransform;
        private Vector2 _releasedPosition;

        private void Awake()
        {
            _scrollRect = GetComponentInParent<ScrollRect>();
            _rectTransform = (RectTransform)transform;
            _releasedPosition = _pressTarget.anchoredPosition;
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.IsInteractable())
                return;

            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            _scrollRect?.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            eventData.eligibleForClick = false;
            _scrollRect?.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _scrollRect?.OnDrag(eventData);
            SetPressed(_button.IsInteractable() && IsPointerInside(eventData));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _scrollRect?.OnEndDrag(eventData);
            SetPressed(false);
        }

        public void Bind(LevelDefinition level, Sprite icon, Action<LevelDefinition> clickHandler)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _clickHandler = clickHandler ?? throw new ArgumentNullException(nameof(clickHandler));

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

        private void SetPressed(bool isPressed)
        {
            _pressTarget.anchoredPosition = isPressed
                ? _releasedPosition + Vector2.down * _pressDepth
                : _releasedPosition;
        }

        private bool IsPointerInside(PointerEventData eventData)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                _rectTransform,
                eventData.position,
                eventData.pressEventCamera);
        }
    }
}
