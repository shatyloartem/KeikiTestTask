using System.Collections.Generic;
using System.IO;
using Core.Levels;
using UnityEngine;

namespace Core.Extensions
{
    public static class LevelCatalogValidator
    {
        public static void Validate(this LevelCatalog catalog)
        {
            if (catalog == null)
                throw new InvalidDataException("Levels JSON does not contain a valid catalog.");

            if (catalog.Categories == null || catalog.Categories.Count == 0)
                throw new InvalidDataException("Levels JSON must contain at least one category.");

            HashSet<string> levelIds = new();

            foreach (LevelCategory category in catalog.Categories)
            {
                if (category == null ||
                    string.IsNullOrWhiteSpace(category.Id) ||
                    string.IsNullOrWhiteSpace(category.Title))
                {
                    throw new InvalidDataException("Every level category must have an id and title.");
                }

                if (category.Levels == null || category.Levels.Count == 0)
                    throw new InvalidDataException($"Category '{category.Id}' does not contain levels.");

                foreach (LevelDefinition level in category.Levels)
                {
                    if (level == null ||
                        string.IsNullOrWhiteSpace(level.Id) ||
                        string.IsNullOrWhiteSpace(level.ColorHex) ||
                        string.IsNullOrWhiteSpace(level.IconAddress))
                    {
                        throw new InvalidDataException(
                            $"Category '{category.Id}' contains an invalid level.");
                    }

                    if (!levelIds.Add(level.Id))
                        throw new InvalidDataException($"Level id '{level.Id}' is duplicated.");

                    if (!ColorUtility.TryParseHtmlString(level.ColorHex, out _))
                        throw new InvalidDataException(
                            $"Level '{level.Id}' has invalid color '{level.ColorHex}'.");
                }
            }
        }
    }
}