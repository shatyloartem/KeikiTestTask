using Core.Configuration;
using Core.SceneManagement;
using Core.StateMachine;
using Runtime.Infrastructure.Storage.Levels;
using Runtime.Services.Levels;
using Runtime.States;
using UnityEngine;
using Zenject;

namespace Runtime.CompositionRoot
{
    public sealed class ApplicationInstaller : MonoInstaller
    {
        private const int DefaultTargetFrameRate = 60;

        [SerializeField, Min(1)]
        private int _targetFrameRate = DefaultTargetFrameRate;

        public override void InstallBindings()
        {
            BindStartupSettings();
            
            BindSceneLoader();

            BindStateMachine();

            BindLevels();
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

        private void BindStateMachine()
        {
            Container.Bind<MenuState>().AsSingle();
            Container.Bind<GameState>().AsSingle();

            Container.Bind<IState>().To<MenuState>().FromResolve().WhenInjectedInto<GameStateMachine>();
            Container.Bind<IState>().To<GameState>().FromResolve().WhenInjectedInto<GameStateMachine>();
            
            Container
                .Bind<IGameStateMachine>()
                .To<GameStateMachine>()
                .AsSingle();
        }

        private void BindLevels()
        {
            Container
                .BindInterfacesAndSelfTo<JsonLevelRepository>()
                .AsSingle();

            Container
                .Bind<ISelectedLevelStore>()
                .To<SelectedLevelStore>()
                .AsSingle();

            Container
                .Bind<ILevelSequenceService>()
                .To<LevelSequenceService>()
                .AsSingle();
        }
    }
}
