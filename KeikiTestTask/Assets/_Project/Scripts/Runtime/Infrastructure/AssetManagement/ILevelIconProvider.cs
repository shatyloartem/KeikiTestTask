using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Runtime.Infrastructure.AssetManagement
{
    public interface ILevelIconProvider : IDisposable
    {
        UniTask<Sprite> LoadAsync(string address, CancellationToken cancellationToken = default);
    }
}
