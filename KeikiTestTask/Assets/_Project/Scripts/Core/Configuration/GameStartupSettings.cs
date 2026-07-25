using System;

namespace Core.Configuration
{
    public sealed class GameStartupSettings
    {
        public GameStartupSettings(int targetFrameRate)
        {
            if (targetFrameRate <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetFrameRate), 
                    "Target frame rate must be greater than zero.");
            }

            TargetFrameRate = targetFrameRate;
        }

        public int TargetFrameRate { get; }
    }
}
