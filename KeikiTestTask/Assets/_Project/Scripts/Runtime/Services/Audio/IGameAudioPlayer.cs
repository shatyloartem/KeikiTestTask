using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Runtime.Services.Audio
{
    public interface IGameAudioPlayer
    {
        UniTask PlayAsync(AudioClip clip, CancellationToken cancellationToken);
    }
}
