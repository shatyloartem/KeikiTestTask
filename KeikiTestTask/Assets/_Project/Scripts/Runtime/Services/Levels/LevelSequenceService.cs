using System;
using Runtime.Domain.Levels;

namespace Runtime.Services.Levels
{
    public sealed class LevelSequenceService : ILevelSequenceService
    {
        public bool TryResolve(LevelCatalog catalog, string levelId, out LevelContext context)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            if (!string.IsNullOrWhiteSpace(levelId))
            {
                foreach (LevelCategory category in catalog.Categories)
                {
                    for (int i = 0; i < category.Levels.Count; i++)
                    {
                        LevelDefinition level = category.Levels[i];

                        if (!string.Equals(level.Id, levelId, StringComparison.Ordinal))
                            continue;

                        context = new LevelContext(category, level, i);
                        return true;
                    }
                }
            }

            context = default;
            return false;
        }

        public LevelContext GetNext(LevelCatalog catalog, LevelContext current)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            int nextIndex = (current.Index + 1) % current.Category.Levels.Count;
            LevelDefinition nextLevel = current.Category.Levels[nextIndex];

            return new LevelContext(current.Category, nextLevel, nextIndex);
        }
    }
}
