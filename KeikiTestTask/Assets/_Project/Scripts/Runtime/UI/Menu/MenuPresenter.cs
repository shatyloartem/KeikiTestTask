using System;
using Core.StateMachine;
using Core.UI;
using Cysharp.Threading.Tasks;
using Runtime.States;
using UnityEngine;

namespace Runtime.UI.Menu
{
    public sealed class MenuPresenter : UIPresenter<MenuView>
    {
        private readonly IGameStateMachine _stateMachine;

        private bool _transitionRequested;

        public MenuPresenter(MenuView view, IGameStateMachine stateMachine)
            : base(view)
        {
            _stateMachine = stateMachine;
        }

        protected override void SubscribeToEvents()
        {
            View.PlayRequested += HandlePlayRequested;
        }

        protected override void UnsubscribeFromEvents()
        {
            View.PlayRequested -= HandlePlayRequested;
        }

        private void HandlePlayRequested()
        {
            if (_transitionRequested || _stateMachine.IsTransitioning)
                return;

            TransitionToGameAsync().Forget();
        }

        private async UniTask TransitionToGameAsync()
        {
            _transitionRequested = true;
            View.SetInteractionEnabled(false);

            try
            {
                await _stateMachine.EnterAsync<GameState>();
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
