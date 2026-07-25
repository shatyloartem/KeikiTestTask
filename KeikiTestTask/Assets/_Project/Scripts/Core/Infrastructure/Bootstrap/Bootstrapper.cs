using System;
using System.Threading;
using Core.Configuration;
using Core.Infrastructure.SceneManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Core.Infrastructure.Bootstrap
{
    public sealed class Bootstrapper : MonoBehaviour
    {
        private GameStartupSettings _startupSettings;
        private ISceneLoader _sceneLoader;

        private CancellationTokenSource _cts;
        
        private bool _started;

        [Inject]
        private void Construct(
            GameStartupSettings startupSettings,
            ISceneLoader sceneLoader)
        {
            _startupSettings = startupSettings;
            _sceneLoader = sceneLoader;
        }

        private void Start()
        {
            if (_started)
                return;

            _started = true;
            
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            RunAsync(_cts.Token).Forget(HandleException);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
        }

        private async UniTask RunAsync(CancellationToken ct)
        {
            EnsureInjected();

            ApplyApplicationSettings();

            await _sceneLoader.LoadAsync(SceneNames.MenuScene, cancellationToken: ct);
        }

        private void EnsureInjected()
        {
            if (_startupSettings == null ||
                _sceneLoader == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(Bootstrapper)} was not injected. " +
                    "Make sure BootScene contains a SceneContext and ProjectContext contains GameInstaller.");
            }
        }

        private void ApplyApplicationSettings()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _startupSettings.TargetFrameRate;
        }
        
        private void HandleException(Exception exception)
        {
            if (exception is OperationCanceledException)
                return;

            Debug.LogException(exception, this);
        }
    }
}
