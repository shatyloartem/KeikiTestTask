using System;
using Runtime.Domain.Levels;

namespace Runtime.Services.Levels
{
    public readonly struct LevelContext
    {
        public LevelContext(
            LevelCategory category,
            LevelDefinition level,
            int index)
        {
            Category = category
                ?? throw new ArgumentNullException(nameof(category));
            Level = level
                ?? throw new ArgumentNullException(nameof(level));
            Index = index;
        }

        public LevelCategory Category { get; }
        public LevelDefinition Level { get; }
        public int Index { get; }
    }
}
