using System;
using System.Collections.Generic;
using Runtime.Domain.Tracing;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Game
{
    public sealed class TraceTrailGraphic : MaskableGraphic
    {
        private const int CapSegments = 12;

        private readonly List<Vector2> _renderPoints = new();

        private TraceStrokeDefinition _stroke;
        private float _widthNormalized;
        private float _progressDistance;

        public void Configure(
            TraceStrokeDefinition stroke,
            float widthNormalized,
            Color trailColor)
        {
            _stroke = stroke ?? throw new ArgumentNullException(nameof(stroke));
            _widthNormalized = Mathf.Max(0.001f, widthNormalized);
            color = trailColor;
            raycastTarget = false;
            _progressDistance = 0f;
            SetVerticesDirty();
        }

        public void SetProgress(float progressDistance)
        {
            if (_stroke == null)
                return;

            float clamped = Mathf.Clamp(progressDistance, 0f, _stroke.TotalLength);

            if (Mathf.Approximately(_progressDistance, clamped))
                return;

            _progressDistance = clamped;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            if (_stroke == null ||
                _stroke.BakedPoints.Count < 2 ||
                _progressDistance <= Mathf.Epsilon)
            {
                return;
            }

            BuildRenderPoints();

            if (_renderPoints.Count < 2)
                return;

            Rect rect = rectTransform.rect;
            float halfWidth = Mathf.Min(rect.width, rect.height) * _widthNormalized * 0.5f;

            for (int i = 0; i < _renderPoints.Count; i++)
            {
                Vector2 current = NormalizedToLocal(_renderPoints[i], rect);
                Vector2 previous = NormalizedToLocal(_renderPoints[Mathf.Max(0, i - 1)], rect);
                Vector2 next = NormalizedToLocal(_renderPoints[Mathf.Min(_renderPoints.Count - 1, i + 1)], rect);
                Vector2 tangent = (next - previous).normalized;
                Vector2 normal = new(-tangent.y, tangent.x);

                AddVertex(vertexHelper, current + normal * halfWidth);
                AddVertex(vertexHelper, current - normal * halfWidth);

                if (i == 0)
                    continue;

                int currentLeft = i * 2;
                int currentRight = currentLeft + 1;
                int previousLeft = currentLeft - 2;
                int previousRight = currentLeft - 1;

                vertexHelper.AddTriangle(previousLeft, currentLeft, currentRight);
                vertexHelper.AddTriangle(previousLeft, currentRight, previousRight);
            }

            Vector2 start = NormalizedToLocal(_renderPoints[0], rect);
            Vector2 end = NormalizedToLocal(_renderPoints[^1], rect);

            AddRoundCap(vertexHelper, start, halfWidth);
            AddRoundCap(vertexHelper, end, halfWidth);
        }

        private void BuildRenderPoints()
        {
            _renderPoints.Clear();

            IReadOnlyList<Vector2> points = _stroke.BakedPoints;
            IReadOnlyList<float> lengths = _stroke.CumulativeLengths;

            _renderPoints.Add(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                if (lengths[i] <= _progressDistance)
                {
                    _renderPoints.Add(points[i]);
                    continue;
                }

                float segmentLength = lengths[i] - lengths[i - 1];
                float t = segmentLength > Mathf.Epsilon
                    ? (_progressDistance - lengths[i - 1]) / segmentLength
                    : 0f;

                _renderPoints.Add(Vector2.LerpUnclamped(points[i - 1], points[i], t));
                break;
            }
        }

        private void AddRoundCap(
            VertexHelper vertexHelper,
            Vector2 center,
            float radius)
        {
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center);

            for (int i = 0; i <= CapSegments; i++)
            {
                float angle = Mathf.PI * 2f * i / CapSegments;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddVertex(vertexHelper, point);

                if (i > 0)
                {
                    vertexHelper.AddTriangle(
                        centerIndex,
                        centerIndex + i,
                        centerIndex + i + 1);
                }
            }
        }

        private void AddVertex(VertexHelper vertexHelper, Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = Vector2.zero;
            vertexHelper.AddVert(vertex);
        }

        private static Vector2 NormalizedToLocal(Vector2 normalized, Rect rect)
        {
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalized.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalized.y));
        }
    }
}
