using System;
using UnityEngine;

namespace Runtime.UI.Game.Tracing
{
    public interface ITraceInputSource
    {
        event Action<Vector2> PointerPressed;
        event Action<Vector2> PointerDragged;
        event Action PointerReleased;
    }
}
