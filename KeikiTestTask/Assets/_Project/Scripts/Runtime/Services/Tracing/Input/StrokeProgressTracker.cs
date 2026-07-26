using System;
using System.Collections.Generic;
using Runtime.Domain.Tracing;
using Runtime.Services.Tracing.Path;
using UnityEngine;

namespace Runtime.Services.Tracing.Input
{
    public sealed class StrokeProgressTracker
    {
        private readonly IReadOnlyList<Vector2> _points;
        private readonly IReadOnlyList<float> _cumulativeLengths;
        private readonly float _startDistance;
        private readonly float _totalLength;
        private readonly float _corridorRadius;
        private readonly float _reacquireTolerance;
        private readonly float _forwardSearchWindow;
        private readonly float _directionEpsilon;
        private readonly float _completionTolerance;

        private bool _pointerActive;
        private bool _tracking;
        private float _lastProjectedDistance;
        private int _segmentHint;

        public StrokeProgressTracker(
            TraceStrokeDefinition stroke,
            float corridorWidth,
            float reacquireTolerance,
            float forwardSearchWindow,
            float directionEpsilon,
            float completionTolerance,
            float inputTolerancePadding = 0f,
            float endInset = 0f,
            float startInset = 0f)
        {
            if (stroke == null)
                throw new ArgumentNullException(nameof(stroke));
            if (stroke.BakedPoints == null ||
                stroke.CumulativeLengths == null ||
                stroke.BakedPoints.Count < 2 ||
                stroke.BakedPoints.Count != stroke.CumulativeLengths.Count)
            {
                throw new ArgumentException("Trace stroke must contain valid baked data.", nameof(stroke));
            }
            if (corridorWidth <= 0f)
                throw new ArgumentOutOfRangeException(nameof(corridorWidth));

            _points = stroke.BakedPoints;
            _cumulativeLengths = stroke.CumulativeLengths;
            _totalLength = Mathf.Max(Mathf.Epsilon, stroke.TotalLength - Mathf.Max(0f, endInset));
            _startDistance = Mathf.Clamp(Mathf.Max(0f, startInset), 0f, Mathf.Max(0f, _totalLength - Mathf.Epsilon));
            _corridorRadius = corridorWidth * 0.5f + Mathf.Max(0f, inputTolerancePadding);
            _reacquireTolerance = Mathf.Max(0f, reacquireTolerance);
            _forwardSearchWindow = Mathf.Max(_reacquireTolerance, forwardSearchWindow);
            _directionEpsilon = Mathf.Max(0f, directionEpsilon);
            _completionTolerance = Mathf.Max(0f, completionTolerance);
            ProgressDistance = _startDistance;
        }

        public float ProgressDistance { get; private set; }
        public float StartDistance => _startDistance;
        public float TargetDistance => _totalLength;
        public float NormalizedProgress =>
            _totalLength - _startDistance > Mathf.Epsilon
            ? (ProgressDistance - _startDistance) / (_totalLength - _startDistance)
            : 0f;
        public bool IsCompleted { get; private set; }

        public TraceSampleResult BeginPointer(Vector2 point)
        {
            if (IsCompleted)
                return CreateResult(TraceSampleStatus.Ignored);

            _pointerActive = true;
            _tracking = false;

            return Evaluate(point);
        }

        public TraceSampleResult Sample(Vector2 point)
        {
            if (!_pointerActive || IsCompleted)
                return CreateResult(TraceSampleStatus.Ignored);

            return Evaluate(point);
        }

        public void EndPointer()
        {
            _pointerActive = false;
            _tracking = false;
        }

        private TraceSampleResult Evaluate(Vector2 point)
        {
            if (!TryProject(point, out Projection projection))
            {
                _tracking = false;
                return CreateResult(TraceSampleStatus.PausedOutsideCorridor);
            }

            if (projection.DistanceToPath > _corridorRadius)
            {
                _tracking = false;
                return CreateResult(TraceSampleStatus.PausedOutsideCorridor);
            }

            if (!_tracking)
            {
                if (projection.PathDistance > ProgressDistance + _reacquireTolerance)
                    return CreateResult(TraceSampleStatus.PausedAhead);

                _tracking = true;
                _lastProjectedDistance = projection.PathDistance;
                _segmentHint = projection.SegmentIndex;

                return CreateResult(TraceSampleStatus.Reacquired);
            }

            if (projection.PathDistance + _directionEpsilon < _lastProjectedDistance)
            {
                _tracking = false;
                return CreateResult(TraceSampleStatus.PausedReverse);
            }

            _lastProjectedDistance = projection.PathDistance;
            _segmentHint = projection.SegmentIndex;

            if (projection.PathDistance <= ProgressDistance + _directionEpsilon)
                return CreateResult(TraceSampleStatus.Ignored);

            ProgressDistance = Mathf.Max(ProgressDistance, projection.PathDistance);

            if (ProgressDistance >= _totalLength - _completionTolerance)
            {
                ProgressDistance = _totalLength;
                IsCompleted = true;
                _pointerActive = false;
                _tracking = false;

                return CreateResult(TraceSampleStatus.Completed);
            }

            return CreateResult(TraceSampleStatus.Advanced);
        }

        private bool TryProject(Vector2 point, out Projection bestProjection)
        {
            float minimumPathDistance = Mathf.Max(0f, ProgressDistance - _reacquireTolerance * 2f);
            float maximumPathDistance = Mathf.Min(_totalLength, ProgressDistance + _forwardSearchWindow);
            float bestSqrDistance = float.PositiveInfinity;
            bool found = false;
            
            int startSegment = Mathf.Max(
                0,
                Mathf.Min(
                    _segmentHint - 2,
                    TracePathUtility.FindSegment(_cumulativeLengths, minimumPathDistance)));
            
            int endSegment = TracePathUtility.FindSegment(
                _cumulativeLengths,
                maximumPathDistance);

            bestProjection = default;

            for (int i = startSegment; i <= endSegment; i++)
            {
                Vector2 start = _points[i];
                Vector2 end = _points[i + 1];
                Vector2 delta = end - start;
                float sqrLength = delta.sqrMagnitude;

                if (sqrLength <= Mathf.Epsilon)
                    continue;

                float t = Mathf.Clamp01(Vector2.Dot(point - start, delta) / sqrLength);
                Vector2 projectedPoint = start + delta * t;
                float sqrDistance = (point - projectedPoint).sqrMagnitude;

                if (sqrDistance >= bestSqrDistance)
                    continue;

                float segmentLength = _cumulativeLengths[i + 1] - _cumulativeLengths[i];
                float pathDistance = _cumulativeLengths[i] + segmentLength * t;

                if (pathDistance < minimumPathDistance - Mathf.Epsilon ||
                    pathDistance > maximumPathDistance + Mathf.Epsilon)
                {
                    continue;
                }

                bestSqrDistance = sqrDistance;
                found = true;
                bestProjection = new Projection(
                    pathDistance,
                    Mathf.Sqrt(sqrDistance),
                    i);
            }

            return found;
        }

        private TraceSampleResult CreateResult(TraceSampleStatus status)
        {
            TracePathUtility.SampleAtDistance(
                _points,
                _cumulativeLengths,
                ProgressDistance,
                out Vector2 position,
                out Vector2 tangent);

            return new TraceSampleResult(
                status,
                ProgressDistance,
                position,
                tangent);
        }

        private readonly struct Projection
        {
            public Projection(
                float pathDistance,
                float distanceToPath,
                int segmentIndex)
            {
                PathDistance = pathDistance;
                DistanceToPath = distanceToPath;
                SegmentIndex = segmentIndex;
            }

            public float PathDistance { get; }
            public float DistanceToPath { get; }
            public int SegmentIndex { get; }
        }
    }
}
