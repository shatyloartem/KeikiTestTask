using UnityEngine;

namespace Runtime.Services.Tracing
{
    public readonly struct TraceSampleResult
    {
        public TraceSampleResult(
            TraceSampleStatus status,
            float progressDistance,
            Vector2 position,
            Vector2 tangent)
        {
            Status = status;
            ProgressDistance = progressDistance;
            Position = position;
            Tangent = tangent;
        }

        public TraceSampleStatus Status { get; }
        public float ProgressDistance { get; }
        public Vector2 Position { get; }
        public Vector2 Tangent { get; }

        public bool ProgressChanged =>
            Status == TraceSampleStatus.Advanced ||
            Status == TraceSampleStatus.Completed;
    }
}
