using System;
using System.Collections.Generic;
using System.Linq;
using Map.Core.Model;

namespace Map.Core.Generation
{
    /// <summary>
    /// Defines deterministic terrain, biome, location, and road generation settings.
    /// </summary>
    public sealed class MapGenerationOptions
    {
        public MapDimensions Dimensions { get; }
        public int Seed { get; }
        public IReadOnlyList<BiomeId> Biomes { get; }
        public IReadOnlyList<BiomeId> BiomeDefinitions => Biomes;
        public string AlgorithmVersion { get; }
        public Guid MapId { get; }
        public NoiseFieldOptions TerrainNoise { get; }
        public NoiseFieldOptions BiomeNoise { get; }
        public IReadOnlyList<BiomeRule> BiomeRules { get; }
        public RoadGenerationPolicy RoadPolicy { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MapGenerationOptions"/>.
        /// </summary>
        /// <param name="mapId">The identifier of the map.</param>
        /// <param name="dimensions">The map dimensions.</param>
        /// <param name="seed">The deterministic seed.</param>
        /// <param name="biomes">The available biome identifiers.</param>
        /// <param name="algorithmVersion">The algorithm version value.</param>
        public MapGenerationOptions(Guid mapId, MapDimensions dimensions, int seed,
            IReadOnlyList<BiomeId> biomes, string algorithmVersion)
            : this(mapId, dimensions, seed, biomes, algorithmVersion, DefaultTerrainNoise, DefaultBiomeNoise,
                CreateDefaultBiomeRules(biomes), DefaultRoadPolicy) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MapGenerationOptions"/>.
        /// </summary>
        /// <param name="dimensions">The map dimensions.</param>
        /// <param name="seed">The deterministic seed.</param>
        /// <param name="biomes">The available biome identifiers.</param>
        /// <param name="algorithmVersion">The algorithm version value.</param>
        /// <param name="mapId">The identifier of the map.</param>
        public MapGenerationOptions(MapDimensions dimensions, int seed,
            IReadOnlyList<BiomeId> biomes, string algorithmVersion, Guid mapId)
            : this(mapId, dimensions, seed, biomes, algorithmVersion) { }

        /// <summary>
        /// Initializes a new instance of <see cref="MapGenerationOptions"/>.
        /// </summary>
        /// <param name="mapId">The identifier of the map.</param>
        /// <param name="dimensions">The map dimensions.</param>
        /// <param name="seed">The deterministic seed.</param>
        /// <param name="biomes">The available biome identifiers.</param>
        /// <param name="algorithmVersion">The algorithm version value.</param>
        /// <param name="terrainNoise">The terrain noise value.</param>
        /// <param name="biomeNoise">The biome noise value.</param>
        /// <param name="biomeRules">The biome rules value.</param>
        /// <param name="roadPolicy">The road policy value.</param>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        public MapGenerationOptions(Guid mapId, MapDimensions dimensions, int seed,
            IReadOnlyList<BiomeId> biomes, string algorithmVersion, NoiseFieldOptions terrainNoise,
            NoiseFieldOptions biomeNoise, IReadOnlyList<BiomeRule> biomeRules, RoadGenerationPolicy roadPolicy)
        {
            if (biomes == null) throw new ArgumentNullException(nameof(biomes));
            if (biomes.Count == 0) throw new ArgumentException("At least one biome is required.", nameof(biomes));
            if (biomes.Any(b => string.IsNullOrWhiteSpace(b.Value)) ||
                biomes.Select(b => b.Value).Distinct(StringComparer.Ordinal).Count() != biomes.Count)
                throw new ArgumentException("Biome definitions must be non-blank and unique.", nameof(biomes));
            if (!MapAlgorithmVersions.IsSupported(algorithmVersion))
                throw new ArgumentException($"Unsupported map algorithm version '{algorithmVersion}'.", nameof(algorithmVersion));
            if (mapId == Guid.Empty) throw new ArgumentException("Map ID cannot be empty.", nameof(mapId));
            if (terrainNoise == null) throw new ArgumentNullException(nameof(terrainNoise));
            if (biomeNoise == null) throw new ArgumentNullException(nameof(biomeNoise));
            if (biomeRules == null) throw new ArgumentNullException(nameof(biomeRules));
            if (roadPolicy == null) throw new ArgumentNullException(nameof(roadPolicy));
            if (biomeRules.Count == 0)
                throw new ArgumentException("At least one biome rule is required.", nameof(biomeRules));

            var declaredBiomes = new HashSet<string>(biomes.Select(biome => biome.Value), StringComparer.Ordinal);
            var ruleBiomes = new HashSet<string>(StringComparer.Ordinal);
            float previousTerrainThreshold = -1f;
            float previousBiomeThreshold = -1f;
            foreach (var rule in biomeRules)
            {
                if (rule == null || !declaredBiomes.Contains(rule.Biome.Value))
                    throw new ArgumentException("Biome rules must use declared biome IDs.", nameof(biomeRules));
                if (rule.MaximumTerrainHeight < previousTerrainThreshold ||
                    (rule.MaximumTerrainHeight == previousTerrainThreshold &&
                     rule.MaximumBiomeNoise < previousBiomeThreshold))
                    throw new ArgumentException("Biome rule thresholds must be ordered.", nameof(biomeRules));
                previousTerrainThreshold = rule.MaximumTerrainHeight;
                previousBiomeThreshold = rule.MaximumBiomeNoise;
                ruleBiomes.Add(rule.Biome.Value);
            }
            if (!declaredBiomes.SetEquals(ruleBiomes))
                throw new ArgumentException("Every declared biome must have at least one rule.", nameof(biomeRules));
            Dimensions = dimensions;
            Seed = seed;
            Biomes = Array.AsReadOnly(new List<BiomeId>(biomes).ToArray());
            AlgorithmVersion = algorithmVersion;
            MapId = mapId;
            TerrainNoise = terrainNoise;
            BiomeNoise = biomeNoise;
            BiomeRules = Array.AsReadOnly(new List<BiomeRule>(biomeRules).ToArray());
            RoadPolicy = roadPolicy;
        }

        private static NoiseFieldOptions DefaultTerrainNoise => new NoiseFieldOptions(24f, 4, 0.5f, 2f, 0f, 0f);
        private static NoiseFieldOptions DefaultBiomeNoise => new NoiseFieldOptions(18f, 3, 0.5f, 2f, 0f, 0f);
        private static RoadGenerationPolicy DefaultRoadPolicy => new RoadGenerationPolicy(true, 1f, 1f);

        /// <summary>
        /// Creates default biome rules.
        /// </summary>
        /// <param name="biomes">The available biome identifiers.</param>
        /// <returns>The resulting collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        private static IReadOnlyList<BiomeRule> CreateDefaultBiomeRules(IReadOnlyList<BiomeId> biomes)
        {
            if (biomes == null) throw new ArgumentNullException(nameof(biomes));
            var rules = new List<BiomeRule>(biomes.Count);
            for (var index = 0; index < biomes.Count; index++)
            {
                var threshold = (index + 1) / (float)biomes.Count;
                rules.Add(new BiomeRule(biomes[index], threshold, threshold));
            }
            return rules;
        }
    }
}
