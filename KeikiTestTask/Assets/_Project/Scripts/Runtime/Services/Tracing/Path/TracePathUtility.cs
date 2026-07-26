using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Services.Tracing.Path
{
    public static class TracePathUtility
    {
        public static void SampleAtDistance(
            IReadOnlyList<Vector2> points,
            IReadOnlyList<float> cumulativeLengths,
            float distance,
            out Vector2 position,
            out Vector2 tangent)
        {
            if (points == null || cumulativeLengths == null)
                throw new ArgumentNullException(points == null ? nameof(points) : nameof(cumulativeLengths));
            if (points.Count < 2 || points.Count != cumulativeLengths.Count)
                throw new ArgumentException("A sampled path needs at least two points and matching lengths.");

            float clampedDistance = Mathf.Clamp(distance, 0f, cumulativeLengths[^1]);
            int segmentIndex = FindSegment(cumulativeLengths, clampedDistance);

            Vector2 start = points[segmentIndex];
            Vector2 end = points[segmentIndex + 1];
            float segmentStart = cumulativeLengths[segmentIndex];
            float segmentLength = cumulativeLengths[segmentIndex + 1] - segmentStart;
            float t = segmentLength > Mathf.Epsilon
                ? (clampedDistance - segmentStart) / segmentLength
                : 0f;

            position = Vector2.LerpUnclamped(start, end, t);
            tangent = (end - start).normalized;
        }

        public static int FindSegment(
            IReadOnlyList<float> cumulativeLengths,
            float distance)
        {
            int low = 0;
            int high = cumulativeLengths.Count - 2;

            while (low <= high)
            {
                int middle = (low + high) / 2;

                if (distance < cumulativeLengths[middle])
                {
                    high = middle - 1;
                }
                else if (distance > cumulativeLengths[middle + 1])
                {
                    low = middle + 1;
                }
                else
                {
                    return middle;
                }
            }

            return Mathf.Clamp(low, 0, cumulativeLengths.Count - 2);
        }
    }
}
