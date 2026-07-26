using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Tracing;

namespace Runtime.Services.Tracing.Input
{
    public interface ITraceInputSession
    {
        event Action UserActivity;

        UniTask TraceAsync(
            TraceGeometryAsset geometry,
            TraceStrokeDefinition stroke,
            CancellationToken cancellationToken);

        bool TryGetProgress(out TraceProgressSnapshot snapshot);

        void Stop();
    }
}
