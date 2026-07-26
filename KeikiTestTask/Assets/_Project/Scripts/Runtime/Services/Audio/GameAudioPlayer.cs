using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Runtime.Services.Audio
{
    public sealed class GameAudioPlayer : IDisposable
    {
        private readonly AudioSource _audioSource;

        private bool _isDisposed;

        public GameAudioPlayer(AudioSource audioSource)
        {
            _audioSource = audioSource
                ? audioSource
                : throw new ArgumentNullException(nameof(audioSource));
        }

        public async UniTask PlayAsync(
            AudioClip clip,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (!clip)
                throw new ArgumentNullException(nameof(clip));

            _audioSource.Stop();
            _audioSource.clip = clip;
            _audioSource.Play();

            try
            {
                while (_audioSource &&
                       _audioSource.isPlaying &&
                       _audioSource.clip == clip)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            finally
            {
                if (cancellationToken.IsCancellationRequested &&
                    _audioSource &&
                    _audioSource.clip == clip)
                {
                    _audioSource.Stop();
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_audioSource)
            {
                _audioSource.Stop();
                _audioSource.clip = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(GameAudioPlayer));
        }
    }
}
