using System;
using Core.UI;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Services.Audio;
using Runtime.Services.Tracing;
using Runtime.Services.Tracing.Assets;
using Runtime.Services.Tracing.Flow;
using Runtime.Services.Tracing.Hints;
using Runtime.UI.Game;
using UnityEngine;
using Zenject;

namespace Runtime.CompositionRoot
{
    public sealed class GameInstaller : MonoInstaller
    {
        [SerializeField] private UIView[] _views;
        [SerializeField] private TraceSurfaceView _traceSurfaceView;
        [SerializeField] private TraceInputView _traceInputView;
        [SerializeField] private AudioSource _audioSource;

        public override void InstallBindings()
        {
            ValidateViews();
            
            ValidateGameplayComponents();
            
            BindViews();
            
            BindGameplay();
            
            BindPresenters();
        }

        private void ValidateViews()
        {
            if (_views == null || _views.Length == 0)
            {
                throw new InvalidOperationException(
                    $"At least one view must be assigned in {nameof(GameInstaller)}.");
            }

            for (int i = 0; i < _views.Length; i++)
            {
                if (!_views[i])
                {
                    throw new InvalidOperationException(
                        $"View at index {i} is not assigned in {nameof(GameInstaller)}.");
                }
            }
        }

        private void ValidateGameplayComponents()
        {
            if (!_traceSurfaceView)
                throw new InvalidOperationException($"{nameof(TraceSurfaceView)} is not assigned.");
            if (!_traceInputView)
                throw new InvalidOperationException($"{nameof(TraceInputView)} is not assigned.");
            if (!_audioSource)
                throw new InvalidOperationException($"{nameof(AudioSource)} is not assigned.");
        }

        private void BindViews()
        {
            foreach (UIView view in _views)
            {
                Container.Bind<UIView>().FromInstance(view);
                Container.Bind(view.GetType()).FromInstance(view);
            }
        }

        private void BindGameplay()
        {
            Container.BindInstance(_traceSurfaceView);
            Container.BindInstance(_traceInputView);
            Container.BindInstance(_audioSource);

            Container
                .BindInterfacesAndSelfTo<AddressableGameAssetProvider>()
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<GameAudioPlayer>()
                .AsSingle();

            Container
                .Bind<TraceAssetLoader>()
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<TraceInputController>()
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<TraceHintController>()
                .AsSingle();

            Container
                .Bind<TraceStrokePlayer>()
                .AsSingle();

            Container
                .BindInterfacesAndSelfTo<GameFlowController>()
                .AsSingle();
        }
        
        private void BindPresenters()
        {
            Container
                .BindInterfacesAndSelfTo<GamePresenter>()
                .AsSingle()
                .NonLazy();
        }
    }
}
