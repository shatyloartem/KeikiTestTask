using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Tracing;
using Runtime.Infrastructure.Storage.Levels;
using Runtime.Services.Audio;
using Runtime.Services.Levels;
using Runtime.Services.Tracing.Assets;
using Runtime.UI.Game;
using Runtime.UI.Game.Tracing;
using UnityEngine;

namespace Runtime.Services.Tracing.Flow
{
    public sealed class GameFlowController : IDisposable
    {
        private readonly ILevelRepository _levelRepository;
        private readonly ISelectedLevelStore _selectedLevelStore;
        private readonly ILevelSequenceService _levelSequenceService;
        private readonly TraceAssetLoader _assetLoader;
        private readonly GameAudioPlayer _audioPlayer;
        private readonly TraceStrokePlayer _strokePlayer;
        private readonly TraceSurfaceView _surfaceView;

        private CancellationTokenSource _runCts;
        private bool _isRunning;
        private bool _isDisposed;

        public event Action LevelReady;

        public GameFlowController(
            ILevelRepository levelRepository,
            ISelectedLevelStore selectedLevelStore,
            ILevelSequenceService levelSequenceService,
            TraceAssetLoader assetLoader,
            GameAudioPlayer audioPlayer,
            TraceStrokePlayer strokePlayer,
            TraceSurfaceView surfaceView)
        {
            _levelRepository = levelRepository
                ?? throw new ArgumentNullException(nameof(levelRepository));
            _selectedLevelStore = selectedLevelStore
                ?? throw new ArgumentNullException(nameof(selectedLevelStore));
            _levelSequenceService = levelSequenceService
                ?? throw new ArgumentNullException(nameof(levelSequenceService));
            _assetLoader = assetLoader
                ?? throw new ArgumentNullException(nameof(assetLoader));
            _audioPlayer = audioPlayer
                ?? throw new ArgumentNullException(nameof(audioPlayer));
            _strokePlayer = strokePlayer
                ?? throw new ArgumentNullException(nameof(strokePlayer));
            _surfaceView = surfaceView
                ? surfaceView
                : throw new ArgumentNullException(nameof(surfaceView));
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
                LevelContext context = ResolveLevel(catalog);
                GameplayAssets gameplayAssets = await _assetLoader.LoadGameplayAsync(catalog.Gameplay, runToken);

                while (true)
                {
                    runToken.ThrowIfCancellationRequested();

                    await PlayLevelAsync(
                        catalog.Gameplay,
                        gameplayAssets,
                        context,
                        runToken);

                    context = _levelSequenceService.GetNext(catalog, context);
                    _selectedLevelStore.Select(context.Level);
                }
            }
            finally
            {
                _strokePlayer.Stop();
                _surfaceView.ClearLevel();
                State = _isDisposed
                    ? GameFlowState.Disposed
                    : GameFlowState.Idle;
                _isRunning = false;
            }
        }

        public void Stop()
        {
            _runCts?.Cancel();
            _strokePlayer.Stop();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();

            _runCts?.Dispose();
            _runCts = null;
            State = GameFlowState.Disposed;
        }

        private LevelContext ResolveLevel(LevelCatalog catalog)
        {
            LevelDefinition selectedLevel = _selectedLevelStore.SelectedLevel;

            if (selectedLevel != null && _levelSequenceService.TryResolve(
                    catalog,
                    selectedLevel.Id,
                    out LevelContext selectedContext))
            {
                return selectedContext;
            }

            LevelCategory firstCategory = catalog.Categories[0];
            LevelContext fallback = new(
                firstCategory,
                firstCategory.Levels[0],
                0);
            _selectedLevelStore.Select(fallback.Level);
            return fallback;
        }

        private async UniTask PlayLevelAsync(
            GameplayDefinition gameplay,
            GameplayAssets gameplayAssets,
            LevelContext context,
            CancellationToken cancellationToken)
        {
            State = GameFlowState.Loading;

            TraceLevelAssets levelAssets = await _assetLoader.LoadLevelAsync(context, cancellationToken);

            _surfaceView.ConfigureLevel(
                levelAssets.Silhouette,
                gameplayAssets.Star,
                gameplayAssets.Mascot,
                gameplayAssets.Helper);
            LevelReady?.Invoke();

            State = GameFlowState.PlayingInstruction;
            await _audioPlayer.PlayAsync(levelAssets.Instruction, cancellationToken);
            
            foreach (TraceStrokeDefinition stroke in levelAssets.Geometry.Strokes)
            {
                await _strokePlayer.PlayAsync(
                    gameplay,
                    levelAssets,
                    stroke,
                    ChangeState,
                    cancellationToken);
            }

            State = GameFlowState.CompletingLevel;

            AudioClip praise = gameplayAssets.PraiseClips[
                UnityEngine.Random.Range(0, gameplayAssets.PraiseClips.Count)];
            
            await _audioPlayer.PlayAsync(praise, cancellationToken);
        }

        private void ChangeState(GameFlowState state)
        {
            State = state;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(GameFlowController));
            }
        }
    }
}
