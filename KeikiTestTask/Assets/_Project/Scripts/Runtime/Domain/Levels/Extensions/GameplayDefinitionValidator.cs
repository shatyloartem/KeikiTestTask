using System.IO;
using System.Linq;

namespace Runtime.Domain.Levels.Extensions
{
    public static class GameplayDefinitionValidator
    {
        public static void Validate(this GameplayDefinition gameplay)
        {
            if (gameplay == null)
                throw new InvalidDataException("Levels JSON does not contain gameplay settings.");

            if (string.IsNullOrWhiteSpace(gameplay.MascotAddress) ||
                string.IsNullOrWhiteSpace(gameplay.RouteStarAddress) ||
                string.IsNullOrWhiteSpace(gameplay.HelperFingerAddress))
            {
                throw new InvalidDataException("Gameplay tracing asset addresses cannot be empty.");
            }

            if (gameplay.PraiseAudioAddresses == null ||
                gameplay.PraiseAudioAddresses.Count == 0 ||
                gameplay.PraiseAudioAddresses.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException(
                    "Gameplay settings must contain at least one valid praise audio address.");
            }

            if (gameplay.RouteRevealDuration <= 0f ||
                gameplay.VoiceHintDelay <= 0f ||
                gameplay.FingerHintDelay <= gameplay.VoiceHintDelay ||
                gameplay.HelperLoopDuration <= 0f)
            {
                throw new InvalidDataException("Gameplay timing settings are invalid.");
            }
        }
    }
}