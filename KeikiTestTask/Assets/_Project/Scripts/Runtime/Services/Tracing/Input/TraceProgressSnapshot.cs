namespace Runtime.Services.Tracing.Input
{
    public readonly struct TraceProgressSnapshot
    {
        public TraceProgressSnapshot(float progressDistance, float targetDistance)
        {
            ProgressDistance = progressDistance;
            TargetDistance = targetDistance;
        }

        public float ProgressDistance { get; }
        public float TargetDistance { get; }
    }
}
