using System;
using UnityEngine;

namespace Runtime.Domain.Tracing
{
    [Serializable]
    public sealed class TraceBezierKnot
    {
        [SerializeField] private Vector2 _position;
        [SerializeField] private Vector2 _inTangent;
        [SerializeField] private Vector2 _outTangent;

        public TraceBezierKnot(
            Vector2 position,
            Vector2 inTangent,
            Vector2 outTangent)
        {
            _position = position;
            _inTangent = inTangent;
            _outTangent = outTangent;
        }

        public Vector2 Position => _position;
        public Vector2 InTangent => _inTangent;
        public Vector2 OutTangent => _outTangent;

        public void SetPosition(Vector2 position)
        {
            _position = position;
        }

        public void SetInTangent(Vector2 tangent)
        {
            _inTangent = tangent;
        }

        public void SetOutTangent(Vector2 tangent)
        {
            _outTangent = tangent;
        }
    }
}
