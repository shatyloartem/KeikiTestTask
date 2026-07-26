using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Runtime.Domain.Tracing
{
    [CreateAssetMenu(
        fileName = "TraceGeometry",
        menuName = "Keiki/Tracing/Geometry")]
    public sealed class TraceGeometryAsset : ScriptableObject
    {
        [SerializeField] private string _geometryId;
        [SerializeField] private Vector2 _referenceSize = new(1000f, 1000f);
        [SerializeField, Range(0.01f, 0.5f)] private float _corridorWidthNormalized = 0.14f;
        [SerializeField, Range(0.005f, 0.5f)] private float _trailWidthNormalized = 0.11f;
        [SerializeField, Range(0.01f, 0.5f)] private float _routePointSpacingNormalized = 0.095f;
        [SerializeField, Range(0f, 0.5f)] private float _firstRoutePointInsetNormalized = 0.095f;
        [SerializeField, Range(0f, 0.5f)] private float _starEndInsetNormalized = 0.035f;
        [SerializeField, Range(0f, 0.25f)] private float _inputTolerancePaddingNormalized = 0.025f;
        [SerializeField, Range(0.001f, 0.25f)] private float _reacquireToleranceNormalized = 0.04f;
        [SerializeField, Range(0.01f, 0.5f)] private float _forwardSearchWindowNormalized = 0.18f;
        [SerializeField, Range(0.0001f, 0.05f)] private float _directionEpsilonNormalized = 0.005f;
        [SerializeField, Range(0.001f, 0.1f)] private float _completionToleranceNormalized = 0.02f;
        [SerializeField] private List<TraceStrokeDefinition> _strokes = new();

        public string GeometryId => _geometryId;
        public Vector2 ReferenceSize => _referenceSize;
        public float CorridorWidthNormalized => _corridorWidthNormalized;
        public float TrailWidthNormalized => _trailWidthNormalized;
        public float RoutePointSpacingNormalized => _routePointSpacingNormalized;
        public float FirstRoutePointInsetNormalized => _firstRoutePointInsetNormalized;
        public float StarEndInsetNormalized => _starEndInsetNormalized;
        public float InputTolerancePaddingNormalized => _inputTolerancePaddingNormalized;
        public float ReacquireToleranceNormalized => _reacquireToleranceNormalized;
        public float ForwardSearchWindowNormalized => _forwardSearchWindowNormalized;
        public float DirectionEpsilonNormalized => _directionEpsilonNormalized;
        public float CompletionToleranceNormalized => _completionToleranceNormalized;
        public IReadOnlyList<TraceStrokeDefinition> Strokes => _strokes;

        public float GetFirstRoutePointInset(TraceStrokeDefinition stroke)
        {
            return stroke is { OverrideRouteInsets: true }
                ? stroke.FirstRoutePointInsetNormalized
                : _firstRoutePointInsetNormalized;
        }

        public float GetStarEndInset(TraceStrokeDefinition stroke)
        {
            return stroke is { OverrideRouteInsets: true }
                ? stroke.StarEndInsetNormalized
                : _starEndInsetNormalized;
        }

        public void Configure(
            string geometryId,
            Vector2 referenceSize,
            float corridorWidthNormalized,
            float trailWidthNormalized,
            float routePointSpacingNormalized,
            float firstRoutePointInsetNormalized,
            float starEndInsetNormalized,
            float inputTolerancePaddingNormalized,
            float reacquireToleranceNormalized,
            float forwardSearchWindowNormalized,
            float directionEpsilonNormalized,
            float completionToleranceNormalized,
            List<TraceStrokeDefinition> strokes)
        {
            _geometryId = geometryId;
            _referenceSize = referenceSize;
            _corridorWidthNormalized = corridorWidthNormalized;
            _trailWidthNormalized = trailWidthNormalized;
            _routePointSpacingNormalized = routePointSpacingNormalized;
            _firstRoutePointInsetNormalized = firstRoutePointInsetNormalized;
            _starEndInsetNormalized = starEndInsetNormalized;
            _inputTolerancePaddingNormalized = inputTolerancePaddingNormalized;
            _reacquireToleranceNormalized = reacquireToleranceNormalized;
            _forwardSearchWindowNormalized = forwardSearchWindowNormalized;
            _directionEpsilonNormalized = directionEpsilonNormalized;
            _completionToleranceNormalized = completionToleranceNormalized;
            _strokes = strokes ?? new List<TraceStrokeDefinition>();
        }

        public void ValidateOrThrow()
        {
            if (string.IsNullOrWhiteSpace(_geometryId))
                throw new InvalidDataException("Trace geometry id cannot be empty.");

            if (_referenceSize.x <= 0f || _referenceSize.y <= 0f)
                throw new InvalidDataException($"Trace geometry '{_geometryId}' has invalid reference size.");

            if (!IsPositiveFinite(_corridorWidthNormalized))
            {
                throw new InvalidDataException(
                    $"Trace geometry '{_geometryId}' has invalid corridor width " +
                    $"({_corridorWidthNormalized}).");
            }

            if (!IsPositiveFinite(_trailWidthNormalized))
            {
                throw new InvalidDataException(
                    $"Trace geometry '{_geometryId}' has invalid trail width " +
                    $"({_trailWidthNormalized}).");
            }

            if (!IsPositiveFinite(_routePointSpacingNormalized))
            {
                throw new InvalidDataException(
                    $"Trace geometry '{_geometryId}' has invalid route point spacing " +
                    $"({_routePointSpacingNormalized}).");
            }

            if (!IsNonNegativeFinite(_firstRoutePointInsetNormalized) ||
                !IsNonNegativeFinite(_starEndInsetNormalized) ||
                !IsNonNegativeFinite(_inputTolerancePaddingNormalized))
            {
                throw new InvalidDataException(
                    $"Trace geometry '{_geometryId}' has invalid route insets or input tolerance.");
            }

            if (_strokes == null || _strokes.Count == 0)
                throw new InvalidDataException($"Trace geometry '{_geometryId}' does not contain strokes.");

            HashSet<string> strokeIds = new();

            foreach (TraceStrokeDefinition stroke in _strokes)
            {
                if (stroke == null ||
                    string.IsNullOrWhiteSpace(stroke.Id) ||
                    !strokeIds.Add(stroke.Id))
                {
                    throw new InvalidDataException(
                        $"Trace geometry '{_geometryId}' contains an invalid or duplicated stroke.");
                }

                if (stroke.BakedPoints == null ||
                    stroke.CumulativeLengths == null ||
                    stroke.BakedPoints.Count < 2 ||
                    stroke.BakedPoints.Count != stroke.CumulativeLengths.Count ||
                    stroke.TotalLength <= Mathf.Epsilon)
                {
                    throw new InvalidDataException(
                        $"Trace stroke '{stroke.Id}' in '{_geometryId}' is not baked.");
                }

                float firstPointInset = GetFirstRoutePointInset(stroke);
                float starEndInset = GetStarEndInset(stroke);

                if (!IsNonNegativeFinite(firstPointInset) ||
                    !IsNonNegativeFinite(starEndInset) ||
                    firstPointInset + starEndInset >= stroke.TotalLength)
                {
                    throw new InvalidDataException(
                        $"Trace stroke '{stroke.Id}' in '{_geometryId}' is shorter than its route insets.");
                }
            }
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private static bool IsNonNegativeFinite(float value)
        {
            return value >= 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }
}
