using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Infrastructure.SceneManagement
{
    public sealed class SceneLoader : ISceneLoader
    {
        public bool IsLoading { get; private set; }

        public async UniTask LoadAsync(
            string sceneName,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException(
                    "Scene name cannot be empty.",
                    nameof(sceneName));
            }

            if (IsLoading)
            {
                throw new InvalidOperationException(
                    $"Cannot load '{sceneName}' because another scene load is already in progress.");
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new InvalidOperationException(
                    $"Scene '{sceneName}' is not enabled in Build Settings.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            IsLoading = true;

            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, loadSceneMode);

                if (operation == null)
                {
                    throw new InvalidOperationException(
                        $"Unity failed to create a load operation for scene '{sceneName}'.");
                }

                await operation.ToUniTask();
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
