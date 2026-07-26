using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Tracing;
using Runtime.Services.Tracing.Assets;
using Runtime.Services.Tracing.Hints;
using Runtime.UI.Game;

namespace Runtime.Services.Tracing.Flow
{
    public sealed class TraceStrokePlayer
    {
        private readonly TraceSurfaceView _surfaceView;
        private readonly TraceInputController _inputController;
        private readonly TraceHintController _hintController;

        public TraceStrokePlayer(
            TraceSurfaceView surfaceView,
            TraceInputController inputController,
            TraceHintController hintController)
        {
            _surfaceView = surfaceView
                ? surfaceView
                : throw new ArgumentNullException(nameof(surfaceView));
            _inputController = inputController
                ?? throw new ArgumentNullException(nameof(inputController));
            _hintController = hintController
                ?? throw new ArgumentNullException(nameof(hintController));
        }

        public async UniTask PlayAsync(
            GameplayDefinition gameplay,
            TraceLevelAssets levelAssets,
            TraceStrokeDefinition stroke,
            Action<GameFlowState> changeState,
            CancellationToken cancellationToken)
        {
            if (gameplay == null)
                throw new ArgumentNullException(nameof(gameplay));
            if (levelAssets == null)
                throw new ArgumentNullException(nameof(levelAssets));
            if (stroke == null)
                throw new ArgumentNullException(nameof(stroke));
            if (changeState == null)
                throw new ArgumentNullException(nameof(changeState));

            changeState(GameFlowState.RevealingStroke);

            using CancellationTokenSource revealCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using CancellationTokenSource hintCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            UniTask revealTask = _surfaceView.RevealStrokeAsync(
                stroke,
                levelAssets.Geometry,
                levelAssets.TraceColor,
                gameplay.RouteRevealDuration,
                revealCts.Token);
            UniTask inputTask = _inputController.TraceAsync(
                levelAssets.Geometry,
                stroke,
                cancellationToken);
            UniTask hintTask = _hintController.RunAsync(
                gameplay,
                levelAssets.Instruction,
                stroke,
                hintCts.Token);

            changeState(GameFlowState.AwaitingInput);

            try
            {
                await inputTask;
            }
            finally
            {
                revealCts.Cancel();
                hintCts.Cancel();

                try
                {
                    await UniTask.WhenAll(revealTask, hintTask);
                }
                catch (OperationCanceledException)
                {
                }

                _hintController.Stop();
            }

            changeState(GameFlowState.CompletingStroke);
            _surfaceView.CompleteActiveStroke();

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        public void Stop()
        {
            _inputController.Stop();
            _hintController.Stop();
        }
    }
}
