using System;

namespace Map.Core.Model
{
    /// <summary>
    /// Describes terrain height and biome at one grid position.
    /// </summary>
    public sealed record MapCell
    {
        public GridPosition Position { get; }
        public float TerrainHeight { get; }
        public BiomeId Biome { get; }
        public BiomeId BiomeId => Biome;

        /// <summary>
        /// Initializes a new instance of <see cref="MapCell"/>.
        /// </summary>
        /// <param name="position">The grid position to process.</param>
        /// <param name="terrainHeight">The terrain height value.</param>
        /// <param name="biome">The biome identifier.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public MapCell(GridPosition position, float terrainHeight, BiomeId biome)
        {
            if (float.IsNaN(terrainHeight) || float.IsInfinity(terrainHeight) || terrainHeight < 0f || terrainHeight > 1f)
                throw new ArgumentOutOfRangeException(nameof(terrainHeight), terrainHeight,
                    "Terrain height must be a finite value between zero and one.");

            Position = position;
            TerrainHeight = terrainHeight;
            Biome = biome;
        }
    }
}
