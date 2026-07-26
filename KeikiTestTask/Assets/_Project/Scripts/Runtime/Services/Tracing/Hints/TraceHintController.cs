using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Tracing;
using Runtime.Services.Audio;
using Runtime.Services.Tracing.Input;
using Runtime.UI.Game;
using Runtime.UI.Game.Tracing;
using UnityEngine;

namespace Runtime.Services.Tracing.Hints
{
    public sealed class TraceHintController : IDisposable
    {
        private readonly GameAudioPlayer _audioPlayer;
        private readonly TraceSurfaceView _surfaceView;
        private readonly TraceInputController _inputController;

        private float _lastActivityTime;
        private int _activityVersion;
        private bool _isActive;
        private bool _isDisposed;

        public TraceHintController(
            GameAudioPlayer audioPlayer,
            TraceSurfaceView surfaceView,
            TraceInputController inputController)
        {
            _audioPlayer = audioPlayer
                ?? throw new ArgumentNullException(nameof(audioPlayer));
            _surfaceView = surfaceView
                ? surfaceView
                : throw new ArgumentNullException(nameof(surfaceView));
            _inputController = inputController
                ?? throw new ArgumentNullException(nameof(inputController));

            _inputController.UserActivity += NotifyUserActivity;
        }

        public async UniTask RunAsync(
            GameplayDefinition gameplay,
            AudioClip instruction,
            TraceStrokeDefinition stroke,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_isActive)
            {
                throw new InvalidOperationException(
                    "A trace hint session is already active.");
            }

            if (gameplay == null)
                throw new ArgumentNullException(nameof(gameplay));
            if (!instruction)
                throw new ArgumentNullException(nameof(instruction));
            if (stroke == null)
                throw new ArgumentNullException(nameof(stroke));

            _isActive = true;
            _lastActivityTime = Time.unscaledTime;
            _activityVersion++;

            int observedVersion = _activityVersion;
            bool voiceShown = false;
            bool fingerShown = false;
            float helperStartTime = 0f;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (observedVersion != _activityVersion)
                    {
                        observedVersion = _activityVersion;
                        voiceShown = false;
                        fingerShown = false;
                        _surfaceView.HideHelper();
                    }

                    float idleDuration = Time.unscaledTime - _lastActivityTime;

                    if (!voiceShown && idleDuration >= gameplay.VoiceHintDelay)
                    {
                        voiceShown = true;
                        RepeatInstructionAsync(instruction, cancellationToken).Forget();
                    }

                    if (!fingerShown && idleDuration >= gameplay.FingerHintDelay)
                    {
                        fingerShown = true;
                        helperStartTime = Time.unscaledTime;
                    }

                    if (fingerShown && _inputController.TryGetProgress(out TraceProgressSnapshot progress)) 
                        ShowHelper(gameplay, stroke, progress, helperStartTime);

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isActive = false;
                _surfaceView.HideHelper();
            }
        }

        public void Stop()
        {
            _isActive = false;
            _surfaceView.HideHelper();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();
            _inputController.UserActivity -= NotifyUserActivity;
        }

        private void NotifyUserActivity()
        {
            if (!_isActive)
                return;

            _lastActivityTime = Time.unscaledTime;
            _activityVersion++;
            _surfaceView.HideHelper();
        }

        private void ShowHelper(
            GameplayDefinition gameplay,
            TraceStrokeDefinition stroke,
            TraceProgressSnapshot progress,
            float helperStartTime)
        {
            float remainingLength = Mathf.Max(0f, progress.TargetDistance - progress.ProgressDistance);
            float normalizedLoopTime = Mathf.Repeat(
                (Time.unscaledTime - helperStartTime) / gameplay.HelperLoopDuration,
                1f);
            
            float helperDistance = progress.ProgressDistance + remainingLength * normalizedLoopTime;

            TracePathUtility.SampleAtDistance(
                stroke.BakedPoints,
                stroke.CumulativeLengths,
                helperDistance,
                out Vector2 helperPosition,
                out _);
            
            _surfaceView.ShowHelperAt(helperPosition);
        }

        private async UniTaskVoid RepeatInstructionAsync(AudioClip instruction, CancellationToken cancellationToken)
        {
            try
            {
                await _audioPlayer.PlayAsync(instruction, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(TraceHintController));
            }
        }
    }
}
