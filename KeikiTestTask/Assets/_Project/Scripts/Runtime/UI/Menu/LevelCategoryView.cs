using System;
using System.Collections.Generic;
using Core.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI.Menu
{
    public sealed class LevelCategoryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private LevelCardView _cardPrefab;

        private readonly List<LevelCardView> _cards = new();

        public void Bind(
            LevelCategory category,
            IReadOnlyDictionary<string, Sprite> icons,
            Action<LevelDefinition> clickHandler)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (icons == null)
                throw new ArgumentNullException(nameof(icons));

            Clear();

            name = $"Category - {category.Id}";
            _title.text = category.Title;

            foreach (LevelDefinition level in category.Levels)
            {
                if (!icons.TryGetValue(level.IconAddress, out Sprite icon))
                {
                    throw new InvalidOperationException(
                        $"Icon '{level.IconAddress}' was not loaded for level '{level.Id}'.");
                }

                LevelCardView card = Instantiate(_cardPrefab, _content);
                card.Bind(level, icon, clickHandler);
                _cards.Add(card);
            }

            _scrollRect.horizontalNormalizedPosition = 0f;
        }
        
        public void SetInteractionEnabled(bool isEnabled)
        {
            foreach (LevelCardView card in _cards)
                card.SetInteractionEnabled(isEnabled);
        }

        private void Clear()
        {
            foreach (LevelCardView card in _cards)
            {
                if (card)
                    Destroy(card.gameObject);
            }

            _cards.Clear();
        }
    }
}
