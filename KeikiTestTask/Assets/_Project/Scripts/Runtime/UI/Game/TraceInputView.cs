using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Runtime.UI.Game
{
    public sealed class TraceInputView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private int? _activePointerId;

        public event Action<Vector2> PointerPressed;
        public event Action<Vector2> PointerDragged;
        public event Action PointerReleased;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId.HasValue)
                return;

            _activePointerId = eventData.pointerId;
            PointerPressed?.Invoke(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_activePointerId != eventData.pointerId)
                return;

            PointerDragged?.Invoke(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_activePointerId != eventData.pointerId)
                return;

            _activePointerId = null;
            PointerReleased?.Invoke();
        }

        private void OnDisable()
        {
            if (!_activePointerId.HasValue)
                return;

            _activePointerId = null;
            PointerReleased?.Invoke();
        }
    }
}
