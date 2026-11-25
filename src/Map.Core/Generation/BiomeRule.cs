using System;
using Map.Core.Model;

namespace Map.Core.Generation
{
    /// <summary>
    /// Maps a maximum normalized terrain height to a biome identifier.
    /// </summary>
    public sealed record BiomeRule
    {
        public BiomeId Biome { get; }
        public BiomeId BiomeId => Biome;
        public float MaximumTerrainHeight { get; }
        public float MaximumBiomeNoise { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="BiomeRule"/>.
        /// </summary>
        /// <param name="biome">The biome identifier.</param>
        /// <param name="maximumTerrainHeight">The maximum terrain height value.</param>
        /// <param name="maximumBiomeNoise">The maximum biome noise value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public BiomeRule(BiomeId biome, float maximumTerrainHeight, float maximumBiomeNoise)
        {
            if (!IsNormalized(maximumTerrainHeight))
                throw new ArgumentOutOfRangeException(nameof(maximumTerrainHeight), maximumTerrainHeight,
                    "Maximum terrain height must be between zero and one.");
            if (!IsNormalized(maximumBiomeNoise))
                throw new ArgumentOutOfRangeException(nameof(maximumBiomeNoise), maximumBiomeNoise,
                    "Maximum biome noise must be between zero and one.");

            Biome = biome;
            MaximumTerrainHeight = maximumTerrainHeight;
            MaximumBiomeNoise = maximumBiomeNoise;
        }

        /// <summary>
        /// Determines whether the value lies in the normalized range.
        /// </summary>
        /// <param name="value">The value to process.</param>
        /// <returns><see langword="true"/> if the value lies in the normalized range; otherwise, <see langword="false"/>.</returns>
        private static bool IsNormalized(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;
    }
}
