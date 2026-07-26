using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Tracing;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Infrastructure.Storage.Levels;
using Runtime.Services.Levels;
using Runtime.UI.Game;
using UnityEngine;

namespace Runtime.Services.Tracing
{
    public sealed class GameFlowController : IDisposable
    {
        private readonly ILevelRepository _levelRepository;
        private readonly ISelectedLevelStore _selectedLevelStore;
        private readonly ILevelSequenceService _levelSequenceService;
        private readonly IGameAssetProvider _assetProvider;
        private readonly GameAudioPlayer _audioPlayer;
        private readonly TraceSurfaceView _surfaceView;
        private readonly TraceInputView _inputView;

        private readonly List<AudioClip> _praiseClips = new();

        private CancellationTokenSource _runCts;
        private CancellationTokenSource _hintCts;
        private UniTaskCompletionSource _strokeCompletion;
        private StrokeProgressTracker _progressTracker;
        private TraceGeometryAsset _activeGeometry;
        private TraceStrokeDefinition _activeStroke;
        private AudioClip _instructionClip;
        private bool _acceptingInput;
        private bool _isRunning;
        private bool _isDisposed;
        private bool _hasPreviousPointerPosition;
        private Vector2 _previousPointerPosition;
        private float _lastActivityTime;
        private int _activityVersion;

        public GameFlowController(
            ILevelRepository levelRepository,
            ISelectedLevelStore selectedLevelStore,
            ILevelSequenceService levelSequenceService,
            IGameAssetProvider assetProvider,
            GameAudioPlayer audioPlayer,
            TraceSurfaceView surfaceView,
            TraceInputView inputView)
        {
            _levelRepository = levelRepository;
            _selectedLevelStore = selectedLevelStore;
            _levelSequenceService = levelSequenceService;
            _assetProvider = assetProvider;
            _audioPlayer = audioPlayer;
            _surfaceView = surfaceView;
            _inputView = inputView;

            _inputView.PointerPressed += HandlePointerPressed;
            _inputView.PointerDragged += HandlePointerDragged;
            _inputView.PointerReleased += HandlePointerReleased;
        }

        public GameFlowState State { get; private set; } = GameFlowState.Idle;

        public async UniTask RunAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_isRunning)
                throw new InvalidOperationException("Game flow is already running.");

            _isRunning = true;
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken runToken = _runCts.Token;

            try
            {
                State = GameFlowState.Loading;

                LevelCatalog catalog = await _levelRepository.LoadAsync(runToken);
                LevelContext context = ResolveInitialLevel(catalog);
                await LoadSharedAssetsAsync(catalog.Gameplay, runToken);

                while (true)
                {
                    runToken.ThrowIfCancellationRequested();

                    await PlayLevelAsync(catalog.Gameplay, context, runToken);

                    context = _levelSequenceService.GetNext(catalog, context);
                    _selectedLevelStore.Select(context.Level);
                }
            }
            finally
            {
                StopActiveStroke();
                _surfaceView.ClearLevel();
                State = _isDisposed ? GameFlowState.Disposed : GameFlowState.Idle;
                _isRunning = false;
            }
        }

        public void Stop()
        {
            _runCts?.Cancel();
            StopActiveStroke();
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

            _runCts?.Dispose();
            _runCts = null;
            State = GameFlowState.Disposed;
        }

        private LevelContext ResolveInitialLevel(LevelCatalog catalog)
        {
            LevelDefinition selectedLevel = _selectedLevelStore.SelectedLevel;

            if (selectedLevel != null &&
                _levelSequenceService.TryResolve(
                    catalog,
                    selectedLevel.Id,
                    out LevelContext selectedContext))
            {
                return selectedContext;
            }

            LevelCategory firstCategory = catalog.Categories[0];
            LevelContext fallback = new(firstCategory, firstCategory.Levels[0], 0);
            _selectedLevelStore.Select(fallback.Level);
            return fallback;
        }

        private async UniTask LoadSharedAssetsAsync(
            GameplayDefinition gameplay,
            CancellationToken cancellationToken)
        {
            _praiseClips.Clear();

            foreach (string address in gameplay.PraiseAudioAddresses)
            {
                AudioClip clip = await _assetProvider.LoadAsync<AudioClip>(
                    address,
                    cancellationToken);
                _praiseClips.Add(clip);
            }
        }

        private async UniTask PlayLevelAsync(
            GameplayDefinition gameplay,
            LevelContext context,
            CancellationToken cancellationToken)
        {
            State = GameFlowState.Loading;

            Sprite silhouette = await _assetProvider.LoadAsync<Sprite>(
                context.Level.SilhouetteAddress,
                cancellationToken);
            TraceGeometryAsset geometry = await _assetProvider.LoadAsync<TraceGeometryAsset>(
                context.Level.TraceGeometryAddress,
                cancellationToken);
            Sprite star = await _assetProvider.LoadAsync<Sprite>(
                gameplay.RouteStarAddress,
                cancellationToken);
            Sprite mascot = await _assetProvider.LoadAsync<Sprite>(
                gameplay.MascotAddress,
                cancellationToken);
            Sprite helper = await _assetProvider.LoadAsync<Sprite>(
                gameplay.HelperFingerAddress,
                cancellationToken);
            _instructionClip = await _assetProvider.LoadAsync<AudioClip>(
                context.Category.InstructionAudioAddress,
                cancellationToken);

            geometry.ValidateOrThrow();

            if (!ColorUtility.TryParseHtmlString(context.Level.ColorHex, out Color traceColor))
            {
                throw new InvalidOperationException(
                    $"Level '{context.Level.Id}' has invalid color '{context.Level.ColorHex}'.");
            }

            _surfaceView.ConfigureLevel(
                silhouette,
                star,
                mascot,
                helper);

            State = GameFlowState.PlayingInstruction;
            await _audioPlayer.PlayAsync(_instructionClip, cancellationToken);

            foreach (TraceStrokeDefinition stroke in geometry.Strokes)
            {
                await PlayStrokeAsync(
                    gameplay,
                    geometry,
                    stroke,
                    traceColor,
                    cancellationToken);
            }

            State = GameFlowState.CompletingLevel;

            AudioClip praise = _praiseClips[
                UnityEngine.Random.Range(0, _praiseClips.Count)];
            await _audioPlayer.PlayAsync(praise, cancellationToken);
        }

        private async UniTask PlayStrokeAsync(
            GameplayDefinition gameplay,
            TraceGeometryAsset geometry,
            TraceStrokeDefinition stroke,
            Color traceColor,
            CancellationToken cancellationToken)
        {
            State = GameFlowState.RevealingStroke;

            await _surfaceView.RevealStrokeAsync(
                stroke,
                geometry,
                traceColor,
                gameplay.RouteRevealDuration,
                cancellationToken);

            _activeGeometry = geometry;
            _activeStroke = stroke;
            _progressTracker = new StrokeProgressTracker(
                stroke,
                geometry.CorridorWidthNormalized,
                geometry.ReacquireToleranceNormalized,
                geometry.ForwardSearchWindowNormalized,
                geometry.DirectionEpsilonNormalized,
                geometry.CompletionToleranceNormalized,
                geometry.InputTolerancePaddingNormalized,
                geometry.GetStarEndInset(stroke),
                geometry.GetFirstRoutePointInset(stroke));
            _strokeCompletion = new UniTaskCompletionSource();
            _acceptingInput = true;
            _hasPreviousPointerPosition = false;
            _lastActivityTime = Time.unscaledTime;
            _activityVersion++;
            State = GameFlowState.AwaitingInput;

            _hintCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            RunHintsAsync(gameplay, _hintCts.Token).Forget();

            try
            {
                await _strokeCompletion.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                _acceptingInput = false;
                _hintCts.Cancel();
                _hintCts.Dispose();
                _hintCts = null;
                _surfaceView.HideHelper();
            }

            State = GameFlowState.CompletingStroke;
            _surfaceView.CompleteActiveStroke();
            _progressTracker = null;
            _activeStroke = null;
            _activeGeometry = null;

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        private async UniTask RunHintsAsync(
            GameplayDefinition gameplay,
            CancellationToken cancellationToken)
        {
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
                        RepeatInstructionAsync(cancellationToken).Forget();
                    }

                    if (!fingerShown && idleDuration >= gameplay.FingerHintDelay)
                    {
                        fingerShown = true;
                        helperStartTime = Time.unscaledTime;
                    }

                    if (fingerShown && _activeStroke != null && _progressTracker != null)
                    {
                        float remainingLength = Mathf.Max(
                            0f,
                            _progressTracker.TargetDistance -
                            _progressTracker.ProgressDistance);
                        float normalizedLoopTime = Mathf.Repeat(
                            (Time.unscaledTime - helperStartTime) /
                            gameplay.HelperLoopDuration,
                            1f);
                        float helperDistance = _progressTracker.ProgressDistance +
                                               remainingLength * normalizedLoopTime;

                        TracePathUtility.SampleAtDistance(
                            _activeStroke.BakedPoints,
                            _activeStroke.CumulativeLengths,
                            helperDistance,
                            out Vector2 helperPosition,
                            out _);
                        _surfaceView.ShowHelperAt(helperPosition);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _surfaceView.HideHelper();
            }
        }

        private async UniTaskVoid RepeatInstructionAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                if (_instructionClip)
                    await _audioPlayer.PlayAsync(_instructionClip, cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void HandlePointerPressed(Vector2 screenPosition)
        {
            NotifyUserActivity();

            if (!_acceptingInput || _progressTracker == null)
                return;

            Vector2 normalized = _surfaceView.ScreenToNormalized(screenPosition);
            _previousPointerPosition = normalized;
            _hasPreviousPointerPosition = true;

            ApplySample(_progressTracker.BeginPointer(normalized));
        }

        private void HandlePointerDragged(Vector2 screenPosition)
        {
            NotifyUserActivity();

            StrokeProgressTracker progressTracker = _progressTracker;
            TraceGeometryAsset activeGeometry = _activeGeometry;

            if (!_acceptingInput ||
                progressTracker == null ||
                activeGeometry == null)
                return;

            Vector2 normalized = _surfaceView.ScreenToNormalized(screenPosition);

            if (!_hasPreviousPointerPosition)
            {
                _previousPointerPosition = normalized;
                _hasPreviousPointerPosition = true;
            }

            float sampleSpacing = Mathf.Max(
                0.0025f,
                activeGeometry.CorridorWidthNormalized * 0.2f);
            float distance = Vector2.Distance(_previousPointerPosition, normalized);
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(distance / sampleSpacing));

            for (int i = 1; i <= sampleCount; i++)
            {
                Vector2 sample = Vector2.Lerp(
                    _previousPointerPosition,
                    normalized,
                    i / (float)sampleCount);
                TraceSampleResult result = progressTracker.Sample(sample);
                ApplySample(result);

                if (result.Status == TraceSampleStatus.Completed)
                    break;
            }

            _previousPointerPosition = normalized;
        }

        private void HandlePointerReleased()
        {
            NotifyUserActivity();
            _hasPreviousPointerPosition = false;
            _progressTracker?.EndPointer();
        }

        private void ApplySample(TraceSampleResult result)
        {
            if (result.ProgressChanged)
                _surfaceView.SetProgress(result);

            if (result.Status == TraceSampleStatus.Completed)
                _strokeCompletion?.TrySetResult();
        }

        private void NotifyUserActivity()
        {
            _lastActivityTime = Time.unscaledTime;
            _activityVersion++;
            _surfaceView.HideHelper();
        }

        private void StopActiveStroke()
        {
            _acceptingInput = false;
            _hasPreviousPointerPosition = false;
            _progressTracker?.EndPointer();
            _strokeCompletion?.TrySetCanceled();
            _hintCts?.Cancel();
            _surfaceView.HideHelper();
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(GameFlowController));
        }
    }
}
