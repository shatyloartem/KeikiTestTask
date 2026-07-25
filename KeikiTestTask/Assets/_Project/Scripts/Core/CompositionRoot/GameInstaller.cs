using Core.Configuration;
using Core.Infrastructure.SceneManagement;
using UnityEngine;
using Zenject;

namespace Core.CompositionRoot
{
    public sealed class GameInstaller : MonoInstaller
    {
        private const int DefaultTargetFrameRate = 60;

        [SerializeField, Min(1)]
        private int _targetFrameRate = DefaultTargetFrameRate;

        public override void InstallBindings()
        {
            BindStartupSettings();
            
            BindSceneLoader();
        }

        private void BindStartupSettings()
        {
            GameStartupSettings startupSettings = new(_targetFrameRate);

            Container.BindInstance(startupSettings).AsSingle();
        }
        
        private void BindSceneLoader()
        {
            Container
                .Bind<ISceneLoader>()
                .To<SceneLoader>()
                .AsSingle();
        }
    }
}
