using Core.Configuration;
using Core.Infrastructure.SceneManagement;
using Core.StateMachine;
using Runtime.States;
using UnityEngine;
using Zenject;

namespace Runtime.Infrastructure.CompositionRoot
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
    }
}
