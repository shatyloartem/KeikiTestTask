using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Tracing;
using Runtime.Services.Tracing.Input;
using UnityEngine;

namespace Runtime.UI.Game.Tracing
{
    public interface ITraceLevelView
    {
        void ConfigureLevel(
            Sprite silhouette,
            Sprite star,
            Sprite mascot,
            Sprite helper);

        void ClearLevel();
    }

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

    public interface ITraceInputSurface
    {
        Vector2 ScreenToNormalized(Vector2 screenPosition);

        void SetProgress(TraceSampleResult result);
    }

    public interface ITraceHintView
    {
        void ShowHelperAt(Vector2 normalizedPosition);

        void HideHelper();
    }
}
