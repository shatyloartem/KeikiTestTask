using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Tracing;
using Runtime.Services.Tracing;
using Runtime.Services.Tracing.Input;
using Runtime.UI.Game.Tracing.Graphics;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Game.Tracing
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TraceSurfaceView : MonoBehaviour
    {
        private const float SilhouetteAlpha = 0.32f;
        private const float RoutePointSizeNormalized = 0.035f;
        private const float StarSizeNormalized = 0.09f;
        private const float MascotSizeNormalized = 0.25f;
        private const float HelperSizeNormalized = 0.18f;

        [SerializeField] private Canvas _canvas;

        private readonly List<TraceTrailGraphic> _trails = new();
        private readonly List<TraceRoutePointGraphic> _routePoints = new();
        private readonly List<float> _routePointDistances = new();

        private RectTransform _surfaceRect;
        private RectTransform _trailLayer;
        private RectTransform _routeLayer;
        private RectTransform _actorLayer;
        private Image _silhouette;
        private Image _star;
        private Image _mascot;
        private Image _helper;
        private TraceTrailGraphic _activeTrail;
        private TraceStrokeDefinition _activeStroke;
        private TraceGeometryAsset _activeGeometry;

        private void Awake()
        {
            _surfaceRect = (RectTransform)transform;

            if (!_canvas)
                _canvas = GetComponentInParent<Canvas>();

            BuildRuntimeHierarchy();
        }

        public Vector2 ScreenToNormalized(Vector2 screenPosition)
        {
            Camera eventCamera = 
                _canvas && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _surfaceRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return new Vector2(float.NaN, float.NaN);
            }

            Rect rect = _surfaceRect.rect;

            return new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y));
        }

        public void ConfigureLevel(
            Sprite silhouette,
            Sprite star,
            Sprite mascot,
            Sprite helper)
        {
            if (!silhouette || !star || !mascot || !helper)
                throw new ArgumentNullException(nameof(silhouette), "Game tracing sprites cannot be null.");

            ClearLevel();

            _silhouette.sprite = silhouette;
            _silhouette.color = new Color(1f, 1f, 1f, SilhouetteAlpha);
            _silhouette.enabled = true;

            _star.sprite = star;
            _mascot.sprite = mascot;
            _helper.sprite = helper;

            _star.enabled = false;
            _mascot.enabled = false;
            _helper.enabled = false;
        }

        public async UniTask RevealStrokeAsync(
            TraceStrokeDefinition stroke,
            TraceGeometryAsset geometry,
            Color traceColor,
            float duration,
            CancellationToken cancellationToken)
        {
            _activeStroke = stroke ?? throw new ArgumentNullException(nameof(stroke));
            _activeGeometry = geometry
                ? geometry
                : throw new ArgumentNullException(nameof(geometry));

            ClearRoute();
            _activeTrail = CreateTrail(stroke, geometry.TrailWidthNormalized, traceColor);

            float firstPointInset =
                geometry.GetFirstRoutePointInset(stroke);
            float starDistance = Mathf.Max(
                0f,
                stroke.TotalLength - geometry.GetStarEndInset(stroke));

            BuildRoutePoints(
                stroke,
                geometry.RoutePointSpacingNormalized,
                firstPointInset,
                starDistance);
            TracePathUtility.SampleAtDistance(
                stroke.BakedPoints,
                stroke.CumulativeLengths,
                starDistance,
                out Vector2 starPosition,
                out _);
            TracePathUtility.SampleAtDistance(
                stroke.BakedPoints,
                stroke.CumulativeLengths,
                firstPointInset,
                out Vector2 mascotPosition,
                out _);
            SetGraphicPosition(_star, starPosition, StarSizeNormalized);
            SetGraphicPosition(_mascot, mascotPosition, MascotSizeNormalized);

            _star.color = WithAlpha(_star.color, 0f);
            _mascot.color = WithAlpha(_mascot.color, 0f);
            _star.enabled = true;
            _mascot.enabled = true;

            foreach (TraceRoutePointGraphic point in _routePoints)
                point.color = WithAlpha(point.color, 0f);

            int revealElementCount = _routePoints.Count + 2;
            float startTime = Time.unscaledTime;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                float normalizedTime = duration <= 0f
                    ? 1f
                    : Mathf.Clamp01((Time.unscaledTime - startTime) / duration);
                float phase = normalizedTime * revealElementCount;

                _mascot.color = WithAlpha(_mascot.color, Mathf.Clamp01(phase));

                for (int i = 0; i < _routePoints.Count; i++)
                {
                    _routePoints[i].color = WithAlpha(
                        _routePoints[i].color,
                        Mathf.Clamp01(phase - i - 1f));
                }

                _star.color = WithAlpha(_star.color, Mathf.Clamp01(phase - revealElementCount + 1f));

                if (normalizedTime >= 1f)
                    break;

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            _mascot.color = WithAlpha(_mascot.color, 1f);
            _star.color = WithAlpha(_star.color, 1f);

            foreach (TraceRoutePointGraphic point in _routePoints)
                point.color = WithAlpha(point.color, 1f);
        }

        public void SetProgress(TraceSampleResult result)
        {
            if (!_activeTrail)
                return;

            _activeTrail.SetProgress(result.ProgressDistance);
            SetGraphicPosition(_mascot, result.Position, MascotSizeNormalized);

            float hiddenBefore = result.ProgressDistance - _activeGeometry.RoutePointSpacingNormalized * 0.4f;

            for (int i = 0; i < _routePoints.Count; i++) 
                _routePoints[i].enabled = _routePointDistances[i] > hiddenBefore;
        }

        public void CompleteActiveStroke()
        {
            if (_activeTrail && _activeStroke != null)
                _activeTrail.SetProgress(_activeStroke.TotalLength);

            ClearRoute();
            _activeTrail = null;
            _activeStroke = null;
            _activeGeometry = null;
        }

        public void ShowHelperAt(Vector2 normalizedPosition)
        {
            SetImagePositionUsingSpritePivot(_helper, normalizedPosition, HelperSizeNormalized);
            _helper.enabled = true;
        }

        public void HideHelper()
        {
            if (_helper)
                _helper.enabled = false;
        }

        public void ClearLevel()
        {
            ClearRoute();

            foreach (TraceTrailGraphic trail in _trails)
            {
                if (trail)
                    Destroy(trail.gameObject);
            }

            _trails.Clear();
            _activeTrail = null;
            _activeStroke = null;
            _activeGeometry = null;

            if (_silhouette)
            {
                _silhouette.sprite = null;
                _silhouette.enabled = false;
            }
        }

        private void BuildRuntimeHierarchy()
        {
            _silhouette = CreateImage("Silhouette", transform);
            Stretch(_silhouette.rectTransform);
            _silhouette.preserveAspect = true;
            _silhouette.enabled = false;

            Mask silhouetteMask = _silhouette.gameObject.AddComponent<Mask>();
            silhouetteMask.showMaskGraphic = true;

            _trailLayer = CreateLayer("Trails", _silhouette.transform);
            _routeLayer = CreateLayer("Route");
            _actorLayer = CreateLayer("Actors");

            _star = CreateImage("Star", _actorLayer);
            _star.preserveAspect = true;
            _star.enabled = false;

            _mascot = CreateImage("Mascot", _actorLayer);
            _mascot.preserveAspect = true;
            _mascot.enabled = false;

            _helper = CreateImage("Helper", _actorLayer);
            _helper.preserveAspect = true;
            _helper.enabled = false;
        }

        private TraceTrailGraphic CreateTrail(TraceStrokeDefinition stroke, float widthNormalized, Color traceColor)
        {
            GameObject obj = new($"Trail - {stroke.Id}", typeof(CanvasRenderer));
            
            RectTransform rectTransform = obj.AddComponent<RectTransform>();
            rectTransform.SetParent(_trailLayer, false);
            Stretch(rectTransform);

            TraceTrailGraphic trail = obj.AddComponent<TraceTrailGraphic>();
            trail.Configure(stroke, widthNormalized, traceColor);
            _trails.Add(trail);

            return trail;
        }

        private void BuildRoutePoints(
            TraceStrokeDefinition stroke,
            float spacing,
            float firstPointInset,
            float starDistance)
        {
            for (float distance = firstPointInset;
                 distance < starDistance - spacing * 0.5f;
                 distance += spacing)
            {
                TracePathUtility.SampleAtDistance(
                    stroke.BakedPoints,
                    stroke.CumulativeLengths,
                    distance,
                    out Vector2 position,
                    out _);

                TraceRoutePointGraphic point =
                    CreateRoutePoint("Route Point", _routeLayer);
                SetGraphicPosition(point, position, RoutePointSizeNormalized);
                _routePoints.Add(point);
                _routePointDistances.Add(distance);
            }
        }

        private void ClearRoute()
        {
            foreach (TraceRoutePointGraphic point in _routePoints)
            {
                if (point)
                    Destroy(point.gameObject);
            }

            _routePoints.Clear();
            _routePointDistances.Clear();

            if (_star)
                _star.enabled = false;
            if (_mascot)
                _mascot.enabled = false;
            if (_helper)
                _helper.enabled = false;
        }

        private RectTransform CreateLayer(string objectName, Transform parent = null)
        {
            GameObject obj = new(objectName, typeof(RectTransform));
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.SetParent(parent ? parent : transform, false);
            Stretch(rectTransform);
            return rectTransform;
        }

        private static Image CreateImage(string objectName, Transform parent)
        {
            GameObject gameObject = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            Image image = gameObject.GetComponent<Image>();
            image.raycastTarget = false;

            return image;
        }

        private static TraceRoutePointGraphic CreateRoutePoint(string objectName, Transform parent)
        {
            GameObject gameObject = new(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TraceRoutePointGraphic));

            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);

            TraceRoutePointGraphic point = gameObject.GetComponent<TraceRoutePointGraphic>();
            point.raycastTarget = false;
            point.color = Color.white;

            return point;
        }

        private void SetGraphicPosition(Graphic graphic, Vector2 normalizedPosition, float sizeNormalized)
        {
            Rect rect = _surfaceRect.rect;
            float size = Mathf.Min(rect.width, rect.height) * sizeNormalized;
            RectTransform graphicRect = graphic.rectTransform;
            graphicRect.anchorMin = Vector2.zero;
            graphicRect.anchorMax = Vector2.zero;
            graphicRect.pivot = new Vector2(0.5f, 0.5f);
            graphicRect.sizeDelta = new Vector2(size, size);
            graphicRect.anchoredPosition = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPosition.x) - rect.xMin,
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPosition.y) - rect.yMin);
        }

        private void SetImagePositionUsingSpritePivot(Image image, Vector2 normalizedPosition, float sizeNormalized)
        {
            Sprite sprite = image.sprite;

            if (!sprite)
            {
                SetGraphicPosition(image, normalizedPosition, sizeNormalized);
                return;
            }

            Rect surfaceRect = _surfaceRect.rect;
            Rect spriteRect = sprite.rect;
            float size = Mathf.Min(surfaceRect.width, surfaceRect.height) *
                         sizeNormalized;
            float aspect = spriteRect.width / spriteRect.height;
            Vector2 sizeDelta = aspect >= 1f
                ? new Vector2(size, size / aspect)
                : new Vector2(size * aspect, size);
            RectTransform imageRect = image.rectTransform;

            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.zero;
            imageRect.pivot = new Vector2(
                sprite.pivot.x / spriteRect.width,
                sprite.pivot.y / spriteRect.height);
            imageRect.sizeDelta = sizeDelta;
            imageRect.anchoredPosition = new Vector2(
                Mathf.Lerp(
                    surfaceRect.xMin,
                    surfaceRect.xMax,
                    normalizedPosition.x) - surfaceRect.xMin,
                Mathf.Lerp(
                    surfaceRect.yMin,
                    surfaceRect.yMax,
                    normalizedPosition.y) - surfaceRect.yMin);
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
