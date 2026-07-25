using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Runtime.UI.Menu
{
    public interface ILevelIconProvider : IDisposable
    {
        UniTask<Sprite> LoadAsync(string address, CancellationToken cancellationToken = default);
    }
}
