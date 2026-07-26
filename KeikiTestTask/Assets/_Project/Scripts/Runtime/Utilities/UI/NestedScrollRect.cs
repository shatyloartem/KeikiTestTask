using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runtime.Utilities.UI
{
    public sealed class NestedScrollRect : ScrollRect
    {
        private ScrollRect _parentScrollRect;
        private bool _routeDragToParent;

        protected override void Awake()
        {
            base.Awake();
            ResolveParentScrollRect();
        }

        public override void OnInitializePotentialDrag(
            PointerEventData eventData)
        {
            base.OnInitializePotentialDrag(eventData);
            ResolveParentScrollRect()?.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            _routeDragToParent = ShouldRouteToParent(eventData.delta);

            if (_routeDragToParent)
                _parentScrollRect.OnBeginDrag(eventData);
            else
                base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (_routeDragToParent)
                _parentScrollRect.OnDrag(eventData);
            else
                base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (_routeDragToParent)
                _parentScrollRect.OnEndDrag(eventData);
            else
                base.OnEndDrag(eventData);

            _routeDragToParent = false;
        }

        public override void OnScroll(PointerEventData eventData)
        {
            if (ShouldRouteToParent(eventData.scrollDelta))
                _parentScrollRect.OnScroll(eventData);
            else
                base.OnScroll(eventData);
        }

        protected override void OnDisable()
        {
            _routeDragToParent = false;
            base.OnDisable();
        }

        private bool ShouldRouteToParent(Vector2 delta)
        {
            ScrollRect parent = ResolveParentScrollRect();

            return parent &&
                   parent.vertical &&
                   Mathf.Abs(delta.y) > Mathf.Abs(delta.x);
        }

        private ScrollRect ResolveParentScrollRect()
        {
            return _parentScrollRect ??=
                transform.parent?.GetComponentInParent<ScrollRect>();
        }
    }
}
