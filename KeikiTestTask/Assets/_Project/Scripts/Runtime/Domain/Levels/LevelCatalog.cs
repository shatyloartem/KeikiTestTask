using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Domain.Levels
{
    [Serializable]
    public sealed class LevelCatalog
    {
        [SerializeField] private int _version;
        [SerializeField] private GameplayDefinition _gameplay;
        [SerializeField] private List<LevelCategory> _categories;

        public LevelCatalog(
            int version,
            GameplayDefinition gameplay,
            List<LevelCategory> categories)
        {
            _version = version;
            _gameplay = gameplay;
            _categories = categories;
        }

        public int Version => _version;
        public GameplayDefinition Gameplay => _gameplay;
        public IReadOnlyList<LevelCategory> Categories => _categories;
    }
}
