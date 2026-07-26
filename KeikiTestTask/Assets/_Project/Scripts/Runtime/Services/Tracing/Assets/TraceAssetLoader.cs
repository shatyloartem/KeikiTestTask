using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Runtime.Domain.Levels;
using Runtime.Domain.Tracing;
using Runtime.Infrastructure.AssetManagement;
using Runtime.Services.Levels;
using UnityEngine;

namespace Runtime.Services.Tracing.Assets
{
    public sealed class TraceAssetLoader
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

            Sprite star = await _assetProvider.LoadAsync<Sprite>(
                gameplay.RouteStarAddress,
                cancellationToken);
            Sprite mascot = await _assetProvider.LoadAsync<Sprite>(
                gameplay.MascotAddress,
                cancellationToken);
            Sprite helper = await _assetProvider.LoadAsync<Sprite>(
                gameplay.HelperFingerAddress,
                cancellationToken);

            List<AudioClip> praiseClips = new(gameplay.PraiseAudioAddresses.Count);

            foreach (string address in gameplay.PraiseAudioAddresses)
            {
                AudioClip clip = await _assetProvider.LoadAsync<AudioClip>(address, cancellationToken);
                praiseClips.Add(clip);
            }

            if (praiseClips.Count == 0)
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
            Sprite silhouette = await _assetProvider.LoadAsync<Sprite>(
                context.Level.SilhouetteAddress,
                cancellationToken);
            TraceGeometryAsset geometry =
                await _assetProvider.LoadAsync<TraceGeometryAsset>(
                    context.Level.TraceGeometryAddress,
                    cancellationToken);
            AudioClip instruction = await _assetProvider.LoadAsync<AudioClip>(
                context.Category.InstructionAudioAddress,
                cancellationToken);

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
