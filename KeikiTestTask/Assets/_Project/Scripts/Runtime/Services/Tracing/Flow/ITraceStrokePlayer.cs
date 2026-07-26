using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Tracing;
using Runtime.Services.Tracing.Assets;

namespace Runtime.Services.Tracing.Flow
{
    public interface ITraceStrokePlayer
    {
        UniTask PlayAsync(
            GameplayDefinition gameplay,
            TraceLevelAssets levelAssets,
            TraceStrokeDefinition stroke,
            Action<GameFlowState> changeState,
            CancellationToken cancellationToken);

        void Stop();
    }
}
