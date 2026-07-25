using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Runtime.Infrastructure.AssetManagement
{
    public sealed class AddressableLevelIconProvider : ILevelIconProvider
    {
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handles = new();
        private readonly Dictionary<string, Sprite> _sprites = new();

        public async UniTask<Sprite> LoadAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Addressable icon address cannot be empty.", nameof(address));

            if (_sprites.TryGetValue(address, out Sprite cachedSprite))
                return cachedSprite;

            AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);

            try
            {
                Sprite sprite = await handle.ToUniTask(cancellationToken: cancellationToken);

                if (!sprite)
                    throw new InvalidOperationException($"Addressable icon '{address}' returned null.");

                _handles.Add(address, handle);
                _sprites.Add(address, sprite);

                return sprite;
            }
            catch
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                throw;
            }
        }

        public void Dispose()
        {
            foreach (AsyncOperationHandle<Sprite> handle in _handles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _handles.Clear();
            _sprites.Clear();
        }
    }
}
