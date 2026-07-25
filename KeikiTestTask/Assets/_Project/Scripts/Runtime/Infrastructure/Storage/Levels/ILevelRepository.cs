using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;

namespace Runtime.Infrastructure.Storage.Levels
{
    public interface ILevelRepository
    {
        UniTask<LevelCatalog> LoadAsync(CancellationToken ct = default);
    }
}
