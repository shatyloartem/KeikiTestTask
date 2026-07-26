using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Services.Levels;

namespace Runtime.Services.Tracing.Assets
{
    public interface ITraceAssetLoader
    {
        UniTask<GameplayAssets> LoadGameplayAsync(GameplayDefinition gameplay, CancellationToken cancellationToken);

        UniTask<TraceLevelAssets> LoadLevelAsync(LevelContext context, CancellationToken cancellationToken);
    }
}
