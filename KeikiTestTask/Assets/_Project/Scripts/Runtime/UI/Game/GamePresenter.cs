using System;
using Core.StateMachine;
using Core.UI;
using Cysharp.Threading.Tasks;
using Runtime.States;
using UnityEngine;

namespace Runtime.UI.Game
{
    public sealed class GamePresenter : UIPresenter<GameView>
    {
        private readonly IGameStateMachine _stateMachine;

        private bool _transitionRequested;

        public GamePresenter(GameView view, IGameStateMachine stateMachine)
            : base(view)
        {
            _stateMachine = stateMachine;
        }

        protected override void SubscribeToEvents()
        {
            View.MenuRequested += HandleMenuRequested;
        }

        protected override void UnsubscribeFromEvents()
        {
            View.MenuRequested -= HandleMenuRequested;
        }

        private void HandleMenuRequested()
        {
            if (_transitionRequested || _stateMachine.IsTransitioning)
                return;

            TransitionToMenuAsync().Forget();
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
