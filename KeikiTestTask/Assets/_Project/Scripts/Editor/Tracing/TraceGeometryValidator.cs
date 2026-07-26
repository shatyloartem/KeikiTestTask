using System.Collections.Generic;
using Runtime.Domain.Tracing;
using UnityEngine;

namespace Editor.Tracing
{
    public static class TraceGeometryValidator
    {
        public static IReadOnlyList<string> Validate(TraceGeometryAsset geometry)
        {
            List<string> messages = new();

            if (!geometry)
            {
                messages.Add("Geometry asset is not selected.");
                return messages;
            }

            try
            {
                geometry.ValidateOrThrow();
            }
            catch (System.Exception exception)
            {
                messages.Add(exception.Message);
            }

            for (int strokeIndex = 0; strokeIndex < geometry.Strokes.Count; strokeIndex++)
            {
                TraceStrokeDefinition stroke = geometry.Strokes[strokeIndex];

                for (int i = 1; i < stroke.BakedPoints.Count; i++)
                {
                    float gap = Vector2.Distance(
                        stroke.BakedPoints[i - 1],
                        stroke.BakedPoints[i]);

                    if (gap > geometry.CorridorWidthNormalized * 0.45f)
                    {
                        messages.Add(
                            $"Stroke '{stroke.Id}' has a baked gap ({gap:F3}) that is too large.");
                        break;
                    }
                }
            }

            int expectedStrokeCount = geometry.GeometryId switch
            {
                "letter-a" => 3,
                "number-1" => 2,
                "shape-circle" => 1,
                _ => -1
            };

            if (expectedStrokeCount >= 0 &&
                geometry.Strokes.Count != expectedStrokeCount)
            {
                messages.Add(
                    $"Geometry '{geometry.GeometryId}' should contain {expectedStrokeCount} strokes.");
            }

            return messages;
        }
    }
}
