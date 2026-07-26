using Runtime.Services.Tracing.Input;
using UnityEngine;

namespace Runtime.UI.Game.Tracing
{
    public interface ITraceInputSurface
    {
        Vector2 ScreenToNormalized(Vector2 screenPosition);

        void SetProgress(TraceSampleResult result);
    }
}
