using Runtime.Domain.Levels;

namespace Runtime.Services.Levels
{
    public interface ISelectedLevelStore
    {
        LevelDefinition SelectedLevel { get; }

        void Select(LevelDefinition level);
    }
}
