using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.StateMachine
{
    public sealed class GameStateMachine : IGameStateMachine
    {
        private readonly IReadOnlyDictionary<Type, IState> _states;

        private IState _currentState;

        public GameStateMachine(List<IState> states)
        {
            if (states == null)
                throw new ArgumentNullException(nameof(states));

            Dictionary<Type, IState> stateMap = MapStates(states);

            _states = stateMap;
        }

        public Type CurrentStateType => _currentState?.GetType();

        public bool IsTransitioning { get; private set; }

        public async UniTask EnterAsync<TState>(
            CancellationToken cancellationToken = default)
            where TState : class, IState
        {
            if (IsTransitioning)
            {
                throw new InvalidOperationException(
                    $"Cannot enter '{typeof(TState).Name}' while another state transition is in progress.");
            }

            if (_currentState is TState)
                return;

            if (!_states.TryGetValue(typeof(TState), out IState nextState))
            {
                throw new InvalidOperationException(
                    $"State '{typeof(TState).Name}' is not registered in {nameof(GameStateMachine)}.");
            }

            IsTransitioning = true;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_currentState != null)
                    await _currentState.ExitAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                _currentState = nextState;
                await _currentState.EnterAsync(cancellationToken);
            }
            catch
            {
                _currentState = null;
                throw;
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        private Dictionary<Type, IState> MapStates(List<IState> states)
        {
            Dictionary<Type, IState> stateMap = new(states.Count);
            
            foreach (IState state in states)
            {
                if (state == null)
                    throw new ArgumentException("State collection cannot contain null.", nameof(states));

                Type stateType = state.GetType();

                if (!stateMap.TryAdd(stateType, state))
                {
                    throw new InvalidOperationException(
                        $"State '{stateType.Name}' is registered more than once.");
                }
            }
            
            return stateMap;
        }
    }
}
