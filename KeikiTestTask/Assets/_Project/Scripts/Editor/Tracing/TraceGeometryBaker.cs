using System;
using System.Collections.Generic;
using Runtime.Domain.Tracing;
using UnityEditor;
using UnityEngine;

namespace Editor.Tracing
{
    internal static class TraceGeometryBaker
    {
        private const int SamplesPerBezierSegment = 64;
        private const float BakedPointSpacing = 0.008f;

        internal static void BakeAsset(TraceGeometryAsset geometry)
        {
            if (!geometry)
                throw new ArgumentNullException(nameof(geometry));

            Undo.RecordObject(geometry, "Bake trace geometry");

            foreach (TraceStrokeDefinition stroke in geometry.Strokes)
                BakeStroke(stroke);

            EditorUtility.SetDirty(geometry);
        }

        internal static void BakeStroke(TraceStrokeDefinition stroke)
        {
            if (stroke == null)
                throw new ArgumentNullException(nameof(stroke));

            if (stroke.Knots == null || stroke.Knots.Count < 2)
                throw new InvalidOperationException($"Stroke '{stroke.Id}' needs at least two Bezier knots.");

            List<Vector2> densePoints = BuildDensePolyline(stroke);
            List<float> denseLengths = BuildCumulativeLengths(densePoints);
            float totalLength = denseLengths[^1];

            if (totalLength <= Mathf.Epsilon)
                throw new InvalidOperationException($"Stroke '{stroke.Id}' has zero length.");

            int pointCount = Mathf.Max(2, Mathf.CeilToInt(totalLength / BakedPointSpacing) + 1);
            List<Vector2> bakedPoints = new(pointCount);
            List<float> bakedLengths = new(pointCount);

            for (int i = 0; i < pointCount; i++)
            {
                float distance = i == pointCount - 1
                    ? totalLength
                    : totalLength * i / (pointCount - 1f);
                bakedPoints.Add(SampleDensePolyline(densePoints, denseLengths, distance));
                bakedLengths.Add(distance);
            }

            stroke.SetBakedData(bakedPoints, bakedLengths);
        }

        private static List<Vector2> BuildDensePolyline(TraceStrokeDefinition stroke)
        {
            IReadOnlyList<TraceBezierKnot> knots = stroke.Knots;
            int segmentCount = stroke.Closed ? knots.Count : knots.Count - 1;
            List<Vector2> points = new(segmentCount * SamplesPerBezierSegment + 1)
            {
                knots[0].Position
            };

            for (int segment = 0; segment < segmentCount; segment++)
            {
                TraceBezierKnot from = knots[segment];
                TraceBezierKnot to = knots[(segment + 1) % knots.Count];
                Vector2 p0 = from.Position;
                Vector2 p1 = from.Position + from.OutTangent;
                Vector2 p2 = to.Position + to.InTangent;
                Vector2 p3 = to.Position;

                for (int sample = 1; sample <= SamplesPerBezierSegment; sample++)
                {
                    float t = sample / (float)SamplesPerBezierSegment;
                    points.Add(EvaluateCubic(p0, p1, p2, p3, t));
                }
            }

            return points;
        }

        private static List<float> BuildCumulativeLengths(IReadOnlyList<Vector2> points)
        {
            List<float> lengths = new(points.Count) { 0f };
            float distance = 0f;

            for (int i = 1; i < points.Count; i++)
            {
                distance += Vector2.Distance(points[i - 1], points[i]);
                lengths.Add(distance);
            }

            return lengths;
        }

        private static Vector2 SampleDensePolyline(
            IReadOnlyList<Vector2> points,
            IReadOnlyList<float> lengths,
            float distance)
        {
            int low = 0;
            int high = lengths.Count - 2;

            while (low <= high)
            {
                int middle = (low + high) / 2;

                if (distance < lengths[middle])
                    high = middle - 1;
                else if (distance > lengths[middle + 1])
                    low = middle + 1;
                else
                {
                    float segmentLength = lengths[middle + 1] - lengths[middle];
                    float t = segmentLength > Mathf.Epsilon
                        ? (distance - lengths[middle]) / segmentLength
                        : 0f;
                    return Vector2.LerpUnclamped(points[middle], points[middle + 1], t);
                }
            }

            return points[^1];
        }

        private static Vector2 EvaluateCubic(
            Vector2 p0,
            Vector2 p1,
            Vector2 p2,
            Vector2 p3,
            float t)
        {
            float oneMinusT = 1f - t;

            return oneMinusT * oneMinusT * oneMinusT * p0 +
                   3f * oneMinusT * oneMinusT * t * p1 +
                   3f * oneMinusT * t * t * p2 +
                   t * t * t * p3;
        }
    }
}
