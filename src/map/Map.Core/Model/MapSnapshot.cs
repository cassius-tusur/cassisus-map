using System;
using System.Collections.Generic;

namespace Map.Core.Model
{
    /// <summary>
    /// Contains immutable generated map cells, location placements, and roads.
    /// </summary>
    public sealed record MapSnapshot
    {
        public Guid MapId { get; }
        public MapDimensions Dimensions { get; }
        public int Seed { get; }
        public string AlgorithmVersion { get; }
        public IReadOnlyList<BiomeId> Biomes { get; }
        public IReadOnlyList<MapCell> Cells { get; }
        public IReadOnlyList<LocationPlacement> Placements { get; }
        public IReadOnlyList<Road> Roads { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MapSnapshot"/>.
        /// </summary>
        /// <param name="mapId">The identifier of the map.</param>
        /// <param name="dimensions">The map dimensions.</param>
        /// <param name="seed">The deterministic seed.</param>
        /// <param name="algorithmVersion">The algorithm version value.</param>
        /// <param name="biomes">The available biome identifiers.</param>
        /// <param name="placements">The placements value.</param>
        /// <param name="roads">The roads value.</param>
        public MapSnapshot(Guid mapId, MapDimensions dimensions, int seed, string algorithmVersion,
            IReadOnlyList<BiomeId> biomes, IReadOnlyList<LocationPlacement> placements, IReadOnlyList<Road> roads)
            : this(mapId, dimensions, seed, algorithmVersion, biomes, CreateDefaultCells(dimensions, biomes), placements, roads)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MapSnapshot"/>.
        /// </summary>
        /// <param name="mapId">The identifier of the map.</param>
        /// <param name="dimensions">The map dimensions.</param>
        /// <param name="seed">The deterministic seed.</param>
        /// <param name="algorithmVersion">The algorithm version value.</param>
        /// <param name="biomes">The available biome identifiers.</param>
        /// <param name="cells">The cells value.</param>
        /// <param name="placements">The placements value.</param>
        /// <param name="roads">The roads value.</param>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        public MapSnapshot(Guid mapId, MapDimensions dimensions, int seed, string algorithmVersion,
            IReadOnlyList<BiomeId> biomes, IReadOnlyList<MapCell> cells, IReadOnlyList<LocationPlacement> placements,
            IReadOnlyList<Road> roads)
        {
            if (mapId == Guid.Empty) throw new ArgumentException("Map ID cannot be empty.", nameof(mapId));
            if (string.IsNullOrWhiteSpace(algorithmVersion))
                throw new ArgumentException("Algorithm version cannot be blank.", nameof(algorithmVersion));
            if (biomes == null) throw new ArgumentNullException(nameof(biomes));
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (placements == null) throw new ArgumentNullException(nameof(placements));
            if (roads == null) throw new ArgumentNullException(nameof(roads));
            if (biomes.Count == 0) throw new ArgumentException("At least one biome is required.", nameof(biomes));

            MapId = mapId;
            Dimensions = dimensions;
            Seed = seed;
            AlgorithmVersion = algorithmVersion;
            Biomes = Array.AsReadOnly(new List<BiomeId>(biomes).ToArray());
            Cells = Array.AsReadOnly(new List<MapCell>(cells).ToArray());
            Placements = Array.AsReadOnly(new List<LocationPlacement>(placements).ToArray());
            Roads = Array.AsReadOnly(new List<Road>(roads).ToArray());
        }

        /// <summary>
        /// Creates the default cell collection for the supplied map dimensions.
        /// </summary>
        /// <param name="dimensions">The map dimensions.</param>
        /// <param name="biomes">The available biome identifiers.</param>
        /// <returns>The resulting collection.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        private static IReadOnlyList<MapCell> CreateDefaultCells(MapDimensions dimensions, IReadOnlyList<BiomeId> biomes)
        {
            if (biomes == null) throw new ArgumentNullException(nameof(biomes));
            if (biomes.Count == 0) throw new ArgumentException("At least one biome is required.", nameof(biomes));

            var cells = new List<MapCell>(checked(dimensions.Width * dimensions.Height));
            for (var y = 0; y < dimensions.Height; y++)
                for (var x = 0; x < dimensions.Width; x++)
                    cells.Add(new MapCell(new GridPosition(x, y), 0f, biomes[0]));
            return cells;
        }
    }
}
