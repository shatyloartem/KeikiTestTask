using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Domain.Tracing
{
    [Serializable]
    public sealed class TraceStrokeDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private bool _closed;
        [SerializeField, HideInInspector] private bool _overrideRouteInsets;
        [SerializeField, HideInInspector] private float _firstRoutePointInsetNormalized = 0.095f;
        [SerializeField, HideInInspector] private float _starEndInsetNormalized = 0.035f;
        [SerializeField] private List<TraceBezierKnot> _knots;
        [SerializeField] private List<Vector2> _bakedPoints = new();
        [SerializeField] private List<float> _cumulativeLengths = new();
        [SerializeField] private float _totalLength;

        public TraceStrokeDefinition(
            string id,
            bool closed,
            List<TraceBezierKnot> knots)
        {
            _id = id;
            _closed = closed;
            _knots = knots ?? new List<TraceBezierKnot>();
        }

        public string Id => _id;
        public bool Closed => _closed;
        public bool OverrideRouteInsets => _overrideRouteInsets;
        public float FirstRoutePointInsetNormalized => _firstRoutePointInsetNormalized;
        public float StarEndInsetNormalized => _starEndInsetNormalized;
        public IReadOnlyList<TraceBezierKnot> Knots => _knots;
        public IReadOnlyList<Vector2> BakedPoints => _bakedPoints;
        public IReadOnlyList<float> CumulativeLengths => _cumulativeLengths;
        public float TotalLength => _totalLength;

        public void Reverse()
        {
            _knots.Reverse();

            if (_overrideRouteInsets)
            {
                (_firstRoutePointInsetNormalized, _starEndInsetNormalized) =
                    (_starEndInsetNormalized, _firstRoutePointInsetNormalized);
            }

            foreach (TraceBezierKnot knot in _knots)
            {
                Vector2 previousIn = knot.InTangent;
                knot.SetInTangent(knot.OutTangent);
                knot.SetOutTangent(previousIn);
            }
        }

        public void ConfigureRouteInsets(
            bool overrideGeometryInsets,
            float firstRoutePointInsetNormalized,
            float starEndInsetNormalized)
        {
            _overrideRouteInsets = overrideGeometryInsets;
            _firstRoutePointInsetNormalized = Mathf.Max(
                0f,
                firstRoutePointInsetNormalized);
            _starEndInsetNormalized = Mathf.Max(0f, starEndInsetNormalized);
        }

        public void SetBakedData(
            List<Vector2> points,
            List<float> cumulativeLengths)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (cumulativeLengths == null)
                throw new ArgumentNullException(nameof(cumulativeLengths));
            if (points.Count != cumulativeLengths.Count)
            {
                throw new ArgumentException(
                    "Baked points and cumulative lengths must have equal counts.");
            }

            _bakedPoints = points;
            _cumulativeLengths = cumulativeLengths;
            _totalLength = cumulativeLengths.Count > 0
                ? cumulativeLengths[^1]
                : 0f;
        }
    }
}
