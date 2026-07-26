using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Levels.Extensions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Runtime.Infrastructure.Storage.Levels
{
    public sealed class JsonLevelRepository : ILevelRepository, IDisposable
    {
        public const string CatalogAddress = "configs/levels";

        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

        private AsyncOperationHandle<TextAsset> _catalogHandle;
        private LevelCatalog _cachedCatalog;
        private bool _isDisposed;

        public async UniTask<LevelCatalog> LoadAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ct.ThrowIfCancellationRequested();

            LevelCatalog cachedCatalog = Volatile.Read(ref _cachedCatalog);
            if (cachedCatalog != null)
                return cachedCatalog;

            await _loadSemaphore.WaitAsync(ct);

            try
            {
                ThrowIfDisposed();

                cachedCatalog = Volatile.Read(ref _cachedCatalog);
                if (cachedCatalog != null)
                    return cachedCatalog;

                AsyncOperationHandle<TextAsset> handle =
                    Addressables.LoadAssetAsync<TextAsset>(CatalogAddress);

                try
                {
                    TextAsset textAsset = await handle.ToUniTask(cancellationToken: ct);

                    if (!textAsset)
                    {
                        throw new InvalidDataException(
                            $"Addressable JSON catalog '{CatalogAddress}' returned null.");
                    }

                    LevelCatalog catalog = JsonUtility.FromJson<LevelCatalog>(textAsset.text);
                    catalog.Validate();

                    _catalogHandle = handle;
                    Volatile.Write(ref _cachedCatalog, catalog);

                    return catalog;
                }
                catch
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);

                    throw;
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Volatile.Write(ref _cachedCatalog, null);

            if (_catalogHandle.IsValid())
                Addressables.Release(_catalogHandle);

            _loadSemaphore.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(JsonLevelRepository));
        }
    }
}
