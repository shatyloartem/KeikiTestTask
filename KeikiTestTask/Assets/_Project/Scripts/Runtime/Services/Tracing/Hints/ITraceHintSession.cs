using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Tracing;
using UnityEngine;

namespace Runtime.Services.Tracing.Hints
{
    public interface ITraceHintSession
    {
        UniTask RunAsync(
            GameplayDefinition gameplay,
            AudioClip instruction,
            TraceStrokeDefinition stroke,
            CancellationToken cancellationToken);

        void Stop();
    }
}
