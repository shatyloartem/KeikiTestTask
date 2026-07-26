namespace Runtime.Services.Tracing
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
