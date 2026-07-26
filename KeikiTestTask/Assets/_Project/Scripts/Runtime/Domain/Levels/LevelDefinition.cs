using System;
using UnityEngine;

namespace Runtime.Domain.Levels
{
    [Serializable]
    public sealed class LevelDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private string _symbol;
        [SerializeField] private string _colorHex;
        [SerializeField] private string _spriteAddress;
        [SerializeField] private string _traceGeometryAddress;

        public LevelDefinition(
            string id,
            string symbol,
            string colorHex,
            string spriteAddress,
            string traceGeometryAddress)
        {
            _id = id;
            _symbol = symbol;
            _colorHex = colorHex;
            _spriteAddress = spriteAddress;
            _traceGeometryAddress = traceGeometryAddress;
        }

        public string Id => _id;
        public string Symbol => _symbol;
        public string ColorHex => _colorHex;
        public string SpriteAddress => _spriteAddress;
        public string TraceGeometryAddress => _traceGeometryAddress;
    }
}
