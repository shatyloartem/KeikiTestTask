using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Runtime.Services.Tracing.Flow
{
    public interface IGameFlow
    {
        event Action LevelReady;

        GameFlowState State { get; }

        UniTask RunAsync(CancellationToken cancellationToken);

        void Stop();
    }
}
