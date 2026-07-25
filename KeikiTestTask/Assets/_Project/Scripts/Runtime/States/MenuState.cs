using System.Threading;
using Core.Infrastructure.SceneManagement;
using Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace Runtime.States
{
    public sealed class MenuState : IState
    {
        private readonly ISceneLoader _sceneLoader;

        public MenuState(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public UniTask EnterAsync(CancellationToken ct = default)
        {
            return _sceneLoader.LoadAsync(
                SceneNames.MenuScene,
                cancellationToken: ct);
        }

        public UniTask ExitAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return UniTask.CompletedTask;
        }
    }
}
