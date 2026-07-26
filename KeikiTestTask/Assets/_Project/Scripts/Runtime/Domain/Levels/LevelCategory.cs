using System;
using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Domain.Levels
{
    [Serializable]
    public sealed class LevelCategory
    {
        [SerializeField] private string _id;
        [SerializeField] private string _title;
        [SerializeField] private string _instructionAudioAddress;
        [SerializeField] private List<LevelDefinition> _levels;

        public LevelCategory(
            string id,
            string title,
            string instructionAudioAddress,
            List<LevelDefinition> levels)
        {
            _id = id;
            _title = title;
            _instructionAudioAddress = instructionAudioAddress;
            _levels = levels;
        }

        public string Id => _id;
        public string Title => _title;
        public string InstructionAudioAddress => _instructionAudioAddress;
        public IReadOnlyList<LevelDefinition> Levels => _levels;
    }
}
