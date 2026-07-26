namespace Runtime.Services.Tracing
{
    public enum GameFlowState
    {
        Idle = 0,
        Loading = 1,
        PlayingInstruction = 2,
        RevealingStroke = 3,
        AwaitingInput = 4,
        CompletingStroke = 5,
        CompletingLevel = 6,
        Disposed = 7
    }
}
