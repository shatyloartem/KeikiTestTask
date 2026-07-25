using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.Levels
{
    public interface ILevelRepository
    {
        UniTask<LevelCatalog> LoadAsync(CancellationToken ct = default);
    }
}
