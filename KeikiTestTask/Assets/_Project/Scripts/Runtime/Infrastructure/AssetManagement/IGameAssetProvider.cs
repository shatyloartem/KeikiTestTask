using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Runtime.Infrastructure.AssetManagement
{
    public interface IGameAssetProvider : IDisposable
    {
        UniTask<T> LoadAsync<T>(string address, CancellationToken cancellationToken = default)
            where T : UnityEngine.Object;
    }
}
