using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Runtime.Infrastructure.AssetManagement
{
    public sealed class AddressableGameAssetProvider : IGameAssetProvider
    {
        private readonly Dictionary<AssetKey, AsyncOperationHandle> _handles = new();
        private readonly Dictionary<AssetKey, UnityEngine.Object> _assets = new();

        private bool _isDisposed;

        public async UniTask<T> LoadAsync<T>(
            string address,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Addressable asset address cannot be empty.", nameof(address));

            AssetKey key = new(address, typeof(T));

            if (_assets.TryGetValue(key, out UnityEngine.Object cachedAsset))
                return (T)cachedAsset;

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);

            try
            {
                T asset = await handle.ToUniTask(cancellationToken: cancellationToken);

                if (!asset)
                {
                    throw new InvalidOperationException(
                        $"Addressable asset '{address}' returned null for type '{typeof(T).Name}'.");
                }

                ThrowIfDisposed();

                _handles.Add(key, handle);
                _assets.Add(key, asset);

                return asset;
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
            if (_isDisposed)
                return;

            _isDisposed = true;

            foreach (AsyncOperationHandle handle in _handles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            _handles.Clear();
            _assets.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AddressableGameAssetProvider));
        }

        private readonly struct AssetKey : IEquatable<AssetKey>
        {
            public AssetKey(string address, Type type)
            {
                Address = address;
                Type = type;
            }

            private string Address { get; }
            private Type Type { get; }

            public bool Equals(AssetKey other)
            {
                return string.Equals(Address, other.Address, StringComparison.Ordinal) &&
                       Type == other.Type;
            }

            public override bool Equals(object obj)
            {
                return obj is AssetKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Address, Type);
            }
        }
    }
}
