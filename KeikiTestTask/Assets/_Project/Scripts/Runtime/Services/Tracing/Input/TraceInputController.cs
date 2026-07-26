using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Tracing;
using Runtime.Services.Tracing.Input;
using Runtime.UI.Game;
using UnityEngine;

namespace Runtime.Services.Tracing
{
    public sealed class TraceInputController : IDisposable
    {
        private readonly TraceSurfaceView _surfaceView;
        private readonly TraceInputView _inputView;

        private UniTaskCompletionSource _completion;
        private StrokeProgressTracker _progressTracker;
        private TraceGeometryAsset _activeGeometry;
        private bool _acceptingInput;
        private bool _hasPreviousPointerPosition;
        private bool _isDisposed;
        private Vector2 _previousPointerPosition;

        public event Action UserActivity;
        
        public TraceInputController(TraceSurfaceView surfaceView, TraceInputView inputView)
        {
            _surfaceView = surfaceView
                ? surfaceView
                : throw new ArgumentNullException(nameof(surfaceView));
            _inputView = inputView
                ? inputView
                : throw new ArgumentNullException(nameof(inputView));

            _inputView.PointerPressed += HandlePointerPressed;
            _inputView.PointerDragged += HandlePointerDragged;
            _inputView.PointerReleased += HandlePointerReleased;
        }

        public async UniTask TraceAsync(
            TraceGeometryAsset geometry,
            TraceStrokeDefinition stroke,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_acceptingInput)
            {
                throw new InvalidOperationException(
                    "A trace input session is already active.");
            }

            if (!geometry)
                throw new ArgumentNullException(nameof(geometry));
            if (stroke == null)
                throw new ArgumentNullException(nameof(stroke));

            _activeGeometry = geometry;
            _progressTracker = CreateProgressTracker(geometry, stroke);
            _completion = new UniTaskCompletionSource();
            _hasPreviousPointerPosition = false;
            _acceptingInput = true;

            try
            {
                await _completion.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                EndSession();
            }
        }

        public bool TryGetProgress(out TraceProgressSnapshot snapshot)
        {
            StrokeProgressTracker tracker = _progressTracker;

            if (!_acceptingInput || tracker == null)
            {
                snapshot = default;
                return false;
            }

            snapshot = new TraceProgressSnapshot(tracker.ProgressDistance, tracker.TargetDistance);
            return true;
        }

        public void Stop()
        {
            if (!_acceptingInput)
                return;

            _acceptingInput = false;
            _completion?.TrySetCanceled();
            EndSession();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();

            _inputView.PointerPressed -= HandlePointerPressed;
            _inputView.PointerDragged -= HandlePointerDragged;
            _inputView.PointerReleased -= HandlePointerReleased;
        }

        private void HandlePointerPressed(Vector2 screenPosition)
        {
            UserActivity?.Invoke();

            StrokeProgressTracker tracker = _progressTracker;

            if (!_acceptingInput || tracker == null)
                return;

            Vector2 normalized = _surfaceView.ScreenToNormalized(screenPosition);
            _previousPointerPosition = normalized;
            _hasPreviousPointerPosition = true;

            ApplySample(tracker.BeginPointer(normalized));
        }

        private void HandlePointerDragged(Vector2 screenPosition)
        {
            UserActivity?.Invoke();

            StrokeProgressTracker tracker = _progressTracker;
            TraceGeometryAsset geometry = _activeGeometry;

            if (!_acceptingInput || tracker == null || !geometry)
                return;

            Vector2 normalized = _surfaceView.ScreenToNormalized(screenPosition);

            if (!_hasPreviousPointerPosition)
            {
                _previousPointerPosition = normalized;
                _hasPreviousPointerPosition = true;
            }

            float sampleSpacing = Mathf.Max(0.0025f, geometry.CorridorWidthNormalized * 0.2f);

            float distance = Vector2.Distance(_previousPointerPosition, normalized);
            
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / sampleSpacing));

            for (int i = 1; i <= sampleCount; i++)
            {
                Vector2 sample = Vector2.Lerp(
                    _previousPointerPosition,
                    normalized,
                    i / (float)sampleCount);

                TraceSampleResult result = tracker.Sample(sample);
                ApplySample(result);

                if (result.Status == TraceSampleStatus.Completed)
                    break;
            }

            _previousPointerPosition = normalized;
        }

        private void HandlePointerReleased()
        {
            UserActivity?.Invoke();
            _hasPreviousPointerPosition = false;
            _progressTracker?.EndPointer();
        }

        private void ApplySample(TraceSampleResult result)
        {
            if (result.ProgressChanged)
                _surfaceView.SetProgress(result);

            if (result.Status == TraceSampleStatus.Completed)
            {
                _acceptingInput = false;
                _completion?.TrySetResult();
            }
        }

        private void EndSession()
        {
            _acceptingInput = false;
            _hasPreviousPointerPosition = false;
            _progressTracker?.EndPointer();
            _progressTracker = null;
            _activeGeometry = null;
            _completion = null;
        }

        private static StrokeProgressTracker CreateProgressTracker(
            TraceGeometryAsset geometry,
            TraceStrokeDefinition stroke)
        {
            return new StrokeProgressTracker(
                stroke,
                geometry.CorridorWidthNormalized,
                geometry.ReacquireToleranceNormalized,
                geometry.ForwardSearchWindowNormalized,
                geometry.DirectionEpsilonNormalized,
                geometry.CompletionToleranceNormalized,
                geometry.InputTolerancePaddingNormalized,
                geometry.GetStarEndInset(stroke),
                geometry.GetFirstRoutePointInset(stroke));
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(TraceInputController));
            }
        }
    }
}
