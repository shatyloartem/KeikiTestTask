using System.Collections.Generic;
using NUnit.Framework;
using Runtime.Domain.Tracing;
using UnityEngine;

namespace Tests.EditMode
{
    public sealed class TraceGeometryValidationTests
    {
        [Test]
        public void TrailMayBeWiderThanInputCorridor()
        {
            TraceGeometryAsset geometry = CreateGeometry(
                corridorWidth: 0.141f,
                trailWidth: 0.24f);

            try
            {
                Assert.DoesNotThrow(geometry.ValidateOrThrow);
            }
            finally
            {
                Object.DestroyImmediate(geometry);
            }
        }

        [Test]
        public void NonPositiveTrailWidthIsRejectedWithSpecificMessage()
        {
            TraceGeometryAsset geometry = CreateGeometry(
                corridorWidth: 0.141f,
                trailWidth: 0f);

            try
            {
                System.IO.InvalidDataException exception = Assert.Throws<
                    System.IO.InvalidDataException>(geometry.ValidateOrThrow);

                StringAssert.Contains("invalid trail width", exception.Message);
            }
            finally
            {
                Object.DestroyImmediate(geometry);
            }
        }

        [Test]
        public void PerStrokeInsetsCanOverrideGeometryDefaultsForShortStroke()
        {
            TraceGeometryAsset geometry = CreateGeometry(
                corridorWidth: 0.141f,
                trailWidth: 0.24f,
                firstPointInset: 0.9f,
                starEndInset: 0.7f);
            ((TraceStrokeDefinition)geometry.Strokes[0]).ConfigureRouteInsets(
                true,
                0.08f,
                0.06f);

            try
            {
                Assert.DoesNotThrow(geometry.ValidateOrThrow);
            }
            finally
            {
                Object.DestroyImmediate(geometry);
            }
        }

        private static TraceGeometryAsset CreateGeometry(
            float corridorWidth,
            float trailWidth,
            float firstPointInset = 0.095f,
            float starEndInset = 0.035f)
        {
            TraceStrokeDefinition stroke = new(
                "stroke",
                false,
                new List<TraceBezierKnot>
                {
                    new(Vector2.zero, Vector2.zero, Vector2.zero),
                    new(Vector2.one, Vector2.zero, Vector2.zero)
                });
            stroke.SetBakedData(
                new List<Vector2> { Vector2.zero, Vector2.one },
                new List<float> { 0f, 1f });

            TraceGeometryAsset geometry =
                ScriptableObject.CreateInstance<TraceGeometryAsset>();
            geometry.Configure(
                "validation-test",
                new Vector2(1000f, 1000f),
                corridorWidth,
                trailWidth,
                0.095f,
                firstPointInset,
                starEndInset,
                0.025f,
                0.035f,
                0.16f,
                0.004f,
                0.018f,
                new List<TraceStrokeDefinition> { stroke });

            return geometry;
        }
    }
}
