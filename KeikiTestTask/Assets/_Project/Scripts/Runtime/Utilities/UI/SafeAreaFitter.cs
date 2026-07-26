using UnityEngine;

namespace Runtime.Utilities.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        private void Apply()
        {
            Rect safeArea = Screen.safeArea;
            Vector2 screenSize = new(Screen.width, Screen.height);

            if (screenSize.x <= 0f || screenSize.y <= 0f)
                return;

            _rectTransform.anchorMin = new Vector2(
                safeArea.xMin / screenSize.x,
                safeArea.yMin / screenSize.y);
            _rectTransform.anchorMax = new Vector2(
                safeArea.xMax / screenSize.x,
                safeArea.yMax / screenSize.y);
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
