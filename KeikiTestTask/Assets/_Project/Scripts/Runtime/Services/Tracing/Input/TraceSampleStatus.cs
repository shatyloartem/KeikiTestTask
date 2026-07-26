namespace Runtime.Services.Tracing.Input
{
    public enum TraceSampleStatus
    {
        Ignored = 0,
        PausedOutsideCorridor = 1,
        PausedReverse = 2,
        PausedAhead = 3,
        Reacquired = 4,
        Advanced = 5,
        Completed = 6
    }
}
