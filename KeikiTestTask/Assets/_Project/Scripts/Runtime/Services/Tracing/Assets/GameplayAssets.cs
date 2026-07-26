using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Services.Tracing.Assets
{
    public sealed class GameplayAssets
    {
        public GameplayAssets(
            Sprite star,
            Sprite mascot,
            Sprite helper,
            IReadOnlyList<AudioClip> praiseClips)
        {
            Star = star
                ? star
                : throw new ArgumentNullException(nameof(star));
            Mascot = mascot
                ? mascot
                : throw new ArgumentNullException(nameof(mascot));
            Helper = helper
                ? helper
                : throw new ArgumentNullException(nameof(helper));
            PraiseClips = praiseClips
                ?? throw new ArgumentNullException(nameof(praiseClips));
        }

        public Sprite Star { get; }
        public Sprite Mascot { get; }
        public Sprite Helper { get; }
        public IReadOnlyList<AudioClip> PraiseClips { get; }
    }
}
