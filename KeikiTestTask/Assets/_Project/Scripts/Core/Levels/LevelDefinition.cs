using System;
using UnityEngine;

namespace Core.Levels
{
    [Serializable]
    public sealed class LevelDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private string _symbol;
        [SerializeField] private string _colorHex;
        [SerializeField] private string _iconAddress;

        public LevelDefinition(
            string id,
            string symbol,
            string colorHex,
            string iconAddress)
        {
            _id = id;
            _symbol = symbol;
            _colorHex = colorHex;
            _iconAddress = iconAddress;
        }

        public string Id => _id;
        public string Symbol => _symbol;
        public string ColorHex => _colorHex;
        public string IconAddress => _iconAddress;
    }
}
