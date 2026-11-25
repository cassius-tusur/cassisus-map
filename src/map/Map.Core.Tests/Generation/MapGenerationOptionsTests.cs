using System;
using System.Collections.Generic;
using Map.Core.Generation;
using Map.Core.Model;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class MapGenerationOptionsTests
    {
        [Fact]
        public void NoiseFieldOptions_RejectsNonPositiveScaleOctavesAndLacunarity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NoiseFieldOptions(0f, 1, 0.5f, 2f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NoiseFieldOptions(1f, 0, 0.5f, 2f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NoiseFieldOptions(1f, 1, 0.5f, 0f, 0f, 0f));
        }

        [Fact]
        public void Options_AcceptsOrderedLegacyBiomeRulesThatReuseForest()
        {
            var biomes = new[]
            {
                new BiomeId("Water"), new BiomeId("Deep Water"), new BiomeId("Sand"),
                new BiomeId("Grassland"), new BiomeId("Forest"), new BiomeId("Jungle"),
                new BiomeId("MountainBase"), new BiomeId("MountainMid"),
                new BiomeId("MountainHigh"), new BiomeId("MountainPeak")
            };
            var rules = new[]
            {
                new BiomeRule(biomes[0], 0.4f, 0.5f),
                new BiomeRule(biomes[1], 0.4f, 1f),
                new BiomeRule(biomes[2], 0.44f, 1f),
                new BiomeRule(biomes[3], 0.6f, 0.5f),
                new BiomeRule(biomes[4], 0.6f, 1f),
                new BiomeRule(biomes[4], 0.69f, 0.5f),
                new BiomeRule(biomes[5], 0.69f, 1f),
                new BiomeRule(biomes[6], 0.8f, 1f),
                new BiomeRule(biomes[7], 0.85f, 1f),
                new BiomeRule(biomes[8], 0.9f, 1f),
                new BiomeRule(biomes[9], 1f, 1f)
            };

            var options = CreateOptions(biomes, rules);

            Assert.Equal(rules, options.BiomeRules);
        }

        [Fact]
        public void Options_RejectsBiomeRulesWithUnorderedThresholds()
        {
            var plains = new BiomeId("plains");
            var forest = new BiomeId("forest");
            var rules = new[]
            {
                new BiomeRule(plains, 0.75f, 0.75f),
                new BiomeRule(forest, 0.5f, 1f)
            };

            Assert.Throws<ArgumentException>(() => CreateOptions(new[] { plains, forest }, rules));
        }

        [Fact]
        public void Options_RejectsDecreasingSecondaryNoiseThresholdWithinTerrainBand()
        {
            var plains = new BiomeId("plains");
            var forest = new BiomeId("forest");
            var rules = new[]
            {
                new BiomeRule(plains, 0.5f, 0.75f),
                new BiomeRule(forest, 0.5f, 0.5f)
            };

            Assert.Throws<ArgumentException>(() => CreateOptions(new[] { plains, forest }, rules));
        }

        [Fact]
        public void Options_DefensivelyCopiesRulesAndPoliciesThatDriveOutput()
        {
            var biome = new BiomeId("plains");
            var biomeRules = new List<BiomeRule> { new BiomeRule(biome, 1f, 1f) };
            var policy = new RoadGenerationPolicy(true, 0.8f, 4f);

            var options = CreateOptions(new[] { biome }, biomeRules, policy);
            biomeRules.Clear();

            Assert.Single(options.BiomeRules);
            Assert.Same(policy, options.RoadPolicy);
            Assert.Equal(0.8f, options.RoadPolicy.PreferredMaximumTerrainHeight);
        }

        private static MapGenerationOptions CreateOptions(
            IReadOnlyList<BiomeId> biomes,
            IReadOnlyList<BiomeRule> biomeRules,
            RoadGenerationPolicy? roadPolicy = null) =>
            new MapGenerationOptions(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                new MapDimensions(2, 2),
                42,
                biomes,
                "v1",
                new NoiseFieldOptions(12f, 3, 0.5f, 2f, 1f, -1f),
                new NoiseFieldOptions(8f, 2, 0.5f, 2f, 0f, 0f),
                biomeRules,
                roadPolicy ?? new RoadGenerationPolicy(true, 1f, 1f));
    }
}
