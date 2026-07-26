namespace Runtime.Services.Tracing.Flow
{
    public enum GameFlowState
    {
        Idle,
        Loading,
        PlayingInstruction,
        RevealingStroke,
        AwaitingInput,
        CompletingStroke,
        CompletingLevel,
        Disposed
    }
}
