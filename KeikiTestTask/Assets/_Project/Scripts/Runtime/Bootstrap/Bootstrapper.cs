using System;
using System.Threading;
using Core.Configuration;
using Core.StateMachine;
using Cysharp.Threading.Tasks;
using Runtime.States;
using UnityEngine;
using Zenject;

namespace Runtime.Bootstrap
{
    public sealed class Bootstrapper : MonoBehaviour
    {
        private GameStartupSettings _startupSettings;
        private IGameStateMachine _stateMachine;

        private CancellationTokenSource _cts;
        
        private bool _started;

        [Inject]
        private void Construct(GameStartupSettings startupSettings, IGameStateMachine stateMachine)
        {
            _startupSettings = startupSettings;
            _stateMachine = stateMachine;
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
            
            await _stateMachine.EnterAsync<MenuState>(ct);
        }

        private void EnsureInjected()
        {
            if (_startupSettings == null || _stateMachine == null)
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
