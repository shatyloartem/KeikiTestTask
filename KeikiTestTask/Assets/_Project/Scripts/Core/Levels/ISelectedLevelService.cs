namespace Core.Levels
{
    public interface ISelectedLevelService
    {
        LevelDefinition SelectedLevel { get; }

        void Select(LevelDefinition level);
    }
}
