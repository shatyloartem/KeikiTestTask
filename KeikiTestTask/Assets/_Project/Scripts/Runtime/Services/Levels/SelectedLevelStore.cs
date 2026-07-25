using System;
using Runtime.Domain.Levels;

namespace Runtime.Services.Levels
{
    public sealed class SelectedLevelStore : ISelectedLevelStore
    {
        public LevelDefinition SelectedLevel { get; private set; }

        public void Select(LevelDefinition level)
        {
            SelectedLevel = level
                ?? throw new ArgumentNullException(nameof(level));
        }
    }
}
