using Runtime.Domain.Levels;

namespace Runtime.Services.Levels
{
    public interface ILevelSequenceService
    {
        bool TryResolve(LevelCatalog catalog, string levelId, out LevelContext context);

        LevelContext GetNext(LevelCatalog catalog, LevelContext current);
    }
}
