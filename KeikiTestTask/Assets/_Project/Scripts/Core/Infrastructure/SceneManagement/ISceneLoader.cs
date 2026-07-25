using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Core.Infrastructure.SceneManagement
{
    public interface ISceneLoader
    {
        bool IsLoading { get; }

        UniTask LoadAsync(
            string sceneName,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            CancellationToken cancellationToken = default);
    }
}
