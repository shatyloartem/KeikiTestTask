using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Tracing;
using UnityEngine;

namespace Runtime.UI.Game.Tracing
{
    public interface ITraceStrokeView
    {
        UniTask RevealStrokeAsync(
            TraceStrokeDefinition stroke,
            TraceGeometryAsset geometry,
            Color traceColor,
            float duration,
            CancellationToken cancellationToken);

        void CompleteActiveStroke();
    }
}
