using System;
using System.Collections.Generic;
using Map.Core.Model;

namespace Map.Core.Generation
{
    /// <summary>
    /// Classifies normalized terrain heights using ordered biome rules.
    /// </summary>
    public static class BiomeClassifier
    {
        /// <summary>
        /// Selects the biome whose rule matches the supplied terrain and noise values.
        /// </summary>
        /// <param name="terrainHeight">The terrain height value.</param>
        /// <param name="biomeNoise">The biome noise value.</param>
        /// <param name="rules">The rules to validate or apply.</param>
        /// <returns>The biome whose rule matches the supplied terrain and noise values.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public static BiomeId Classify(float terrainHeight, float biomeNoise, IReadOnlyList<BiomeRule> rules)
        {
            if (!IsNormalized(terrainHeight))
                throw new ArgumentOutOfRangeException(nameof(terrainHeight), terrainHeight,
                    "Terrain height must be a finite value between zero and one.");
            if (!IsNormalized(biomeNoise))
                throw new ArgumentOutOfRangeException(nameof(biomeNoise), biomeNoise,
                    "Biome noise must be a finite value between zero and one.");
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            for (var index = 0; index < rules.Count; index++)
            {
                var rule = rules[index] ?? throw new ArgumentException("Biome rules cannot contain null.", nameof(rules));
                if (terrainHeight <= rule.MaximumTerrainHeight &&
                    (biomeNoise < rule.MaximumBiomeNoise || rule.MaximumBiomeNoise == 1f))
                    return rule.Biome;
            }

            throw new ArgumentException("No biome rule matches the supplied terrain height and biome noise.", nameof(rules));
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
