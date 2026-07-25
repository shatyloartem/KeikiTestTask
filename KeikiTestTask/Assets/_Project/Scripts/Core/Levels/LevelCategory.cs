using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Levels
{
    [Serializable]
    public sealed class LevelCategory
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private List<LevelDefinition> _levels;

        public LevelCategory(
            string id,
            string title,
            List<LevelDefinition> levels)
        {
            _id = id;
            _title = title;
            _levels = levels;
        }

        public string Id => _id;
        public string Title => _title;
        public IReadOnlyList<LevelDefinition> Levels => _levels;
    }
}
