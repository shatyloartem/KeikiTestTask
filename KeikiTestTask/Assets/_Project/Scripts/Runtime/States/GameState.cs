using System.Threading;
using Core.SceneManagement;
using Core.StateMachine;
using Cysharp.Threading.Tasks;

namespace Runtime.States
{
    public sealed class GameState : IState
    {
        private readonly ISceneLoader _sceneLoader;

        public GameState(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public UniTask EnterAsync(CancellationToken ct = default)
        {
            return _sceneLoader.LoadAsync(
                SceneNames.GameScene,
                cancellationToken: ct);
        }

        public UniTask ExitAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return UniTask.CompletedTask;
        }
    }
}
