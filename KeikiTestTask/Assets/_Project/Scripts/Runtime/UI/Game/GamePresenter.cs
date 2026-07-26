using System;
using System.Threading;
using Core.StateMachine;
using Core.UI;
using Cysharp.Threading.Tasks;
using Runtime.Services.Tracing.Flow;
using Runtime.States;
using UnityEngine;

namespace Runtime.UI.Game
{
    public sealed class GamePresenter : UIPresenter<GameView>
    {
        private readonly IGameStateMachine _stateMachine;
        private readonly GameFlowController _gameFlowController;

        private CancellationTokenSource _flowCts;
        private bool _transitionRequested;

        public GamePresenter(
            GameView view,
            IGameStateMachine stateMachine,
            GameFlowController gameFlowController)
            : base(view)
        {
            _stateMachine = stateMachine;
            _gameFlowController = gameFlowController;
        }

        protected override void SubscribeToEvents()
        {
            View.MenuRequested += HandleMenuRequested;
        }

        protected override void UnsubscribeFromEvents()
        {
            View.MenuRequested -= HandleMenuRequested;
        }

        protected override void OnInitialized()
        {
            _flowCts = CancellationTokenSource.CreateLinkedTokenSource(View.LifetimeToken);
            RunGameAsync(_flowCts.Token).Forget();
        }

        protected override void OnDisposed()
        {
            _flowCts?.Cancel();
            _flowCts?.Dispose();
            _flowCts = null;
            _gameFlowController.Stop();
        }

        private void HandleMenuRequested()
        {
            if (_transitionRequested || _stateMachine.IsTransitioning)
                return;

            _flowCts?.Cancel();
            _gameFlowController.Stop();
            TransitionToMenuAsync().Forget();
        }

        private async UniTask RunGameAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _gameFlowController.RunAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, View);
            }
        }

        private async UniTask TransitionToMenuAsync()
        {
            _transitionRequested = true;
            View.SetInteractionEnabled(false);

            try
            {
                await _stateMachine.EnterAsync<MenuState>();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, View);
            }
            finally
            {
                _transitionRequested = false;

                if (View)
                    View.SetInteractionEnabled(true);
            }
        }
    }
}
