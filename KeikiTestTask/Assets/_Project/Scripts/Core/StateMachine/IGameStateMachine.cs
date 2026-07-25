using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.StateMachine
{
    public interface IGameStateMachine
    {
        Type CurrentStateType { get; }

        bool IsTransitioning { get; }

        UniTask EnterAsync<TState>(CancellationToken cancellationToken = default)
            where TState : class, IState;
    }
}
