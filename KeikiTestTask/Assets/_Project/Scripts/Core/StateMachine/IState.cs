using System.Threading;
using Cysharp.Threading.Tasks;

namespace Core.StateMachine
{
    public interface IState
    {
        UniTask EnterAsync(CancellationToken ct = default);

        UniTask ExitAsync(CancellationToken ct = default);
    }
}
