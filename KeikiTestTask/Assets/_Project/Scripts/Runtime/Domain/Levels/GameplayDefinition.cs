using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Domain.Levels
{
    [Serializable]
    public sealed class GameplayDefinition
    {
        [SerializeField] private string _mascotAddress;
        [SerializeField] private string _routeStarAddress;
        [SerializeField] private string _helperFingerAddress;
        [SerializeField] private List<string> _praiseAudioAddresses;
        [SerializeField, Min(0.01f)] private float _routeRevealDuration = 1f;
        [SerializeField, Min(0.01f)] private float _voiceHintDelay = 7f;
        [SerializeField, Min(0.01f)] private float _fingerHintDelay = 14f;
        [SerializeField, Min(0.01f)] private float _helperLoopDuration = 2.5f;

        public GameplayDefinition(
            string mascotAddress,
            string routeStarAddress,
            string helperFingerAddress,
            List<string> praiseAudioAddresses,
            float routeRevealDuration,
            float voiceHintDelay,
            float fingerHintDelay,
            float helperLoopDuration)
        {
            _mascotAddress = mascotAddress;
            _routeStarAddress = routeStarAddress;
            _helperFingerAddress = helperFingerAddress;
            _praiseAudioAddresses = praiseAudioAddresses;
            _routeRevealDuration = routeRevealDuration;
            _voiceHintDelay = voiceHintDelay;
            _fingerHintDelay = fingerHintDelay;
            _helperLoopDuration = helperLoopDuration;
        }

        public string MascotAddress => _mascotAddress;
        public string RouteStarAddress => _routeStarAddress;
        public string HelperFingerAddress => _helperFingerAddress;
        public IReadOnlyList<string> PraiseAudioAddresses => _praiseAudioAddresses;
        public float RouteRevealDuration => _routeRevealDuration;
        public float VoiceHintDelay => _voiceHintDelay;
        public float FingerHintDelay => _fingerHintDelay;
        public float HelperLoopDuration => _helperLoopDuration;
    }
}
