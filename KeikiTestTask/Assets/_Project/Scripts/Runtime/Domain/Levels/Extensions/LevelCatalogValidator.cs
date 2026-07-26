using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Runtime.Domain.Levels.Extensions
{
    public static class LevelCatalogValidator
    {
        public static void Validate(this LevelCatalog catalog)
        {
            if (catalog == null)
                throw new InvalidDataException("Levels JSON does not contain a valid catalog.");

            if (catalog.Categories == null || catalog.Categories.Count == 0)
                throw new InvalidDataException("Levels JSON must contain at least one category.");

            ValidateGameplay(catalog.Gameplay);

            HashSet<string> categoryIds = new();
            HashSet<string> levelIds = new();

            foreach (LevelCategory category in catalog.Categories)
            {
                if (category == null ||
                    string.IsNullOrWhiteSpace(category.Id) ||
                    string.IsNullOrWhiteSpace(category.Title) ||
                    string.IsNullOrWhiteSpace(category.InstructionAudioAddress))
                {
                    throw new InvalidDataException(
                        "Every level category must have an id, title and instruction audio address.");
                }

                if (!categoryIds.Add(category.Id))
                    throw new InvalidDataException($"Category id '{category.Id}' is duplicated.");

                if (category.Levels == null || category.Levels.Count == 0)
                    throw new InvalidDataException($"Category '{category.Id}' does not contain levels.");

                foreach (LevelDefinition level in category.Levels)
                {
                    if (level == null ||
                        string.IsNullOrWhiteSpace(level.Id) ||
                        string.IsNullOrWhiteSpace(level.ColorHex) ||
                        string.IsNullOrWhiteSpace(level.IconAddress) ||
                        string.IsNullOrWhiteSpace(level.SilhouetteAddress) ||
                        string.IsNullOrWhiteSpace(level.TraceGeometryAddress))
                    {
                        throw new InvalidDataException(
                            $"Category '{category.Id}' contains an invalid level.");
                    }

                    if (!levelIds.Add(level.Id))
                        throw new InvalidDataException($"Level id '{level.Id}' is duplicated.");

                    if (!ColorUtility.TryParseHtmlString(level.ColorHex, out _))
                    {
                        throw new InvalidDataException(
                            $"Level '{level.Id}' has invalid color '{level.ColorHex}'.");
                    }
                }
            }
        }

        private static void ValidateGameplay(GameplayDefinition gameplay)
        {
            if (gameplay == null)
                throw new InvalidDataException("Levels JSON does not contain gameplay settings.");

            if (string.IsNullOrWhiteSpace(gameplay.MascotAddress) ||
                string.IsNullOrWhiteSpace(gameplay.RouteStarAddress) ||
                string.IsNullOrWhiteSpace(gameplay.HelperFingerAddress))
            {
                throw new InvalidDataException("Gameplay tracing asset addresses cannot be empty.");
            }

            if (gameplay.PraiseAudioAddresses == null ||
                gameplay.PraiseAudioAddresses.Count == 0 ||
                gameplay.PraiseAudioAddresses.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException(
                    "Gameplay settings must contain at least one valid praise audio address.");
            }

            if (gameplay.RouteRevealDuration <= 0f ||
                gameplay.VoiceHintDelay <= 0f ||
                gameplay.FingerHintDelay <= gameplay.VoiceHintDelay ||
                gameplay.HelperLoopDuration <= 0f)
            {
                throw new InvalidDataException("Gameplay timing settings are invalid.");
            }
        }
    }
}
