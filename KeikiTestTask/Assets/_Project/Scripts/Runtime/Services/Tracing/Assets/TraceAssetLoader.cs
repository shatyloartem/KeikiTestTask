using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Tracing;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Services.Levels;
using UnityEngine;

namespace Runtime.Services.Tracing.Assets
{
    public sealed class TraceAssetLoader : ITraceAssetLoader
    {
        private readonly IGameAssetProvider _assetProvider;

        public TraceAssetLoader(IGameAssetProvider assetProvider)
        {
            _assetProvider = assetProvider
                ?? throw new ArgumentNullException(nameof(assetProvider));
        }

        public async UniTask<GameplayAssets> LoadGameplayAsync(
            GameplayDefinition gameplay,
            CancellationToken cancellationToken)
        {
            if (gameplay == null)
                throw new ArgumentNullException(nameof(gameplay));

            UniTask<Sprite> starTask = _assetProvider.LoadAsync<Sprite>(
                gameplay.RouteStarAddress,
                cancellationToken);
            UniTask<Sprite> mascotTask = _assetProvider.LoadAsync<Sprite>(
                gameplay.MascotAddress,
                cancellationToken);
            UniTask<Sprite> helperTask = _assetProvider.LoadAsync<Sprite>(
                gameplay.HelperFingerAddress,
                cancellationToken);
            UniTask<AudioClip[]> praiseTask = UniTask.WhenAll(
                gameplay.PraiseAudioAddresses.Select(
                    address => _assetProvider.LoadAsync<AudioClip>(
                        address,
                        cancellationToken)));

            (Sprite star, Sprite mascot, Sprite helper, AudioClip[] praiseClips) =
                await UniTask.WhenAll(
                    starTask,
                    mascotTask,
                    helperTask,
                    praiseTask);

            if (praiseClips.Length == 0)
            {
                throw new InvalidOperationException(
                    "Gameplay must define at least one praise audio clip.");
            }

            return new GameplayAssets(
                star,
                mascot,
                helper,
                praiseClips);
        }

        public async UniTask<TraceLevelAssets> LoadLevelAsync(LevelContext context, CancellationToken cancellationToken)
        {
            UniTask<Sprite> spriteTask = _assetProvider.LoadAsync<Sprite>(
                context.Level.SpriteAddress,
                cancellationToken);
            UniTask<TraceGeometryAsset> geometryTask =
                _assetProvider.LoadAsync<TraceGeometryAsset>(
                context.Level.TraceGeometryAddress,
                cancellationToken);
            UniTask<AudioClip> instructionTask = _assetProvider.LoadAsync<AudioClip>(
                context.Category.InstructionAudioAddress,
                cancellationToken);

            (Sprite silhouette, TraceGeometryAsset geometry, AudioClip instruction) =
                await UniTask.WhenAll(
                    spriteTask,
                    geometryTask,
                    instructionTask);

            geometry.Validate();

            if (!ColorUtility.TryParseHtmlString(context.Level.ColorHex, out Color traceColor))
            {
                throw new InvalidOperationException(
                    $"Level '{context.Level.Id}' has invalid color " +
                    $"'{context.Level.ColorHex}'.");
            }

            return new TraceLevelAssets(
                silhouette,
                geometry,
                instruction,
                traceColor);
        }
    }
}
