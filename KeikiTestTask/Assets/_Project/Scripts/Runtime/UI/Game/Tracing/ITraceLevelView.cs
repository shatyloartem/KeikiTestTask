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
}
