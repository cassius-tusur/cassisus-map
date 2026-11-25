using System;

namespace Map.Core.Generation
{
    /// <summary>
    /// Defines whether and how story locations are connected by generated roads.
    /// </summary>
    public sealed record RoadGenerationPolicy
    {
        public bool ConnectFeatures { get; }
        public float PreferredMaximumTerrainHeight { get; }
        public float TerrainHeightCost { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RoadGenerationPolicy"/>.
        /// </summary>
        /// <param name="connectFeatures">The connect features value.</param>
        /// <param name="preferredMaximumTerrainHeight">The preferred maximum terrain height value.</param>
        /// <param name="terrainHeightCost">The terrain height cost value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public RoadGenerationPolicy(bool connectFeatures, float preferredMaximumTerrainHeight, float terrainHeightCost)
        {
            if (float.IsNaN(preferredMaximumTerrainHeight) || float.IsInfinity(preferredMaximumTerrainHeight) ||
                preferredMaximumTerrainHeight < 0f || preferredMaximumTerrainHeight > 1f)
                throw new ArgumentOutOfRangeException(nameof(preferredMaximumTerrainHeight), preferredMaximumTerrainHeight,
                    "Preferred maximum terrain height must be between zero and one.");
            if (float.IsNaN(terrainHeightCost) || float.IsInfinity(terrainHeightCost) || terrainHeightCost < 0f)
                throw new ArgumentOutOfRangeException(nameof(terrainHeightCost), terrainHeightCost,
                    "Terrain height cost must be finite and non-negative.");

            ConnectFeatures = connectFeatures;
            PreferredMaximumTerrainHeight = preferredMaximumTerrainHeight;
            TerrainHeightCost = terrainHeightCost;
        }
    }
}
