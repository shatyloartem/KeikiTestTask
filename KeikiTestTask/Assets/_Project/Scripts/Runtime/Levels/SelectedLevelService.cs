using System;
using Core.Levels;

namespace Runtime.Levels
{
    public sealed class SelectedLevelService : ISelectedLevelService
    {
        public LevelDefinition SelectedLevel { get; private set; }

        public void Select(LevelDefinition level)
        {
            SelectedLevel = level
                ?? throw new ArgumentNullException(nameof(level));
        }
    }
}
