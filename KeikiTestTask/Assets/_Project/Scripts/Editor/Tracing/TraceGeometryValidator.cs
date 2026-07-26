using System;
using System.Collections.Generic;
using Runtime.Domain.Tracing;
using UnityEngine;

namespace Editor.Tracing
{
    internal static class TraceGeometryValidator
    {
        private const float MaximumGapToCorridorRatio = 0.45f;

        internal static IReadOnlyList<string> Validate(TraceGeometryAsset geometry)
        {
            List<string> messages = new();

            if (!geometry)
            {
                messages.Add("Geometry asset is not selected.");
                return messages;
            }

            try
            {
                geometry.Validate();
            }
            catch (Exception exception)
            {
                messages.Add(exception.Message);
                return messages;
            }

            foreach (TraceStrokeDefinition stroke in geometry.Strokes)
            {
                for (int i = 1; i < stroke.BakedPoints.Count; i++)
                {
                    float gap = Vector2.Distance(
                        stroke.BakedPoints[i - 1],
                        stroke.BakedPoints[i]);

                    if (gap > geometry.CorridorWidthNormalized * MaximumGapToCorridorRatio)
                    {
                        messages.Add(
                            $"Stroke '{stroke.Id}' has a baked gap ({gap:F3}) that is too large.");
                        break;
                    }
                }
            }

            return messages;
        }
    }
}
