using System;
using Runtime.Domain.Tracing;
using UnityEngine;

namespace Runtime.Services.Tracing.Assets
{
    public sealed class TraceLevelAssets
    {
        public TraceLevelAssets(
            Sprite silhouette,
            TraceGeometryAsset geometry,
            AudioClip instruction,
            Color traceColor)
        {
            Silhouette = silhouette
                ? silhouette
                : throw new ArgumentNullException(nameof(silhouette));
            Geometry = geometry
                ? geometry
                : throw new ArgumentNullException(nameof(geometry));
            Instruction = instruction
                ? instruction
                : throw new ArgumentNullException(nameof(instruction));
            TraceColor = traceColor;
        }

        public Sprite Silhouette { get; }
        public TraceGeometryAsset Geometry { get; }
        public AudioClip Instruction { get; }
        public Color TraceColor { get; }
    }
}
