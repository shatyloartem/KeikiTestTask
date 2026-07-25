using System;
using System.Collections.Generic;
using Core.Levels;
using Core.UI;
using UnityEngine;

namespace Runtime.UI.Menu
{
    public sealed class MenuView : UIView
    {
        [SerializeField] private RectTransform _levelsContent;
        [SerializeField] private LevelCategoryView _categoryPrefab;

        private readonly List<LevelCategoryView> _categories = new();

        public event Action<LevelDefinition> LevelSelected;

        public void RenderCatalog(
            LevelCatalog catalog,
            IReadOnlyDictionary<string, Sprite> icons)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            Clear();

            foreach (LevelCategory category in catalog.Categories)
            {
                LevelCategoryView categoryView = Instantiate(_categoryPrefab, _levelsContent);

                categoryView.Bind(category, icons, NotifyLevelSelected);
                _categories.Add(categoryView);
            }
        }

        public override void SetInteractionEnabled(bool isEnabled)
        {
            base.SetInteractionEnabled(isEnabled);

            foreach (LevelCategoryView category in _categories)
                category.SetInteractionEnabled(isEnabled);
        }

        private void NotifyLevelSelected(LevelDefinition level)
        {
            LevelSelected?.Invoke(level);
        }

        private void Clear()
        {
            foreach (LevelCategoryView category in _categories)
            {
                if (category)
                    Destroy(category.gameObject);
            }

            _categories.Clear();
        }
    }
}
