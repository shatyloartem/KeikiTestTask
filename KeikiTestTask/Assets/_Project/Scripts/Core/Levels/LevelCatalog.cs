using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Levels
{
    [Serializable]
    public sealed class LevelCatalog
    {
        [SerializeField] private int _version;
        [SerializeField] private List<LevelCategory> _categories;

        public LevelCatalog(int version, List<LevelCategory> categories)
        {
            _version = version;
            _categories = categories;
        }

        public int Version => _version;
        public IReadOnlyList<LevelCategory> Categories => _categories;
    }
}
