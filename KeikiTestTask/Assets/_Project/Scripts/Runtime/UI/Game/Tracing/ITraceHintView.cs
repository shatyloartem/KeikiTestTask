using UnityEngine;

namespace Runtime.UI.Game.Tracing
{
    public interface ITraceHintView
    {
        void ShowHelperAt(Vector2 normalizedPosition);

        void HideHelper();
    }
}
