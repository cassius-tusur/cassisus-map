using System.Collections.Generic;
using Map.Core.Generation;
using Map.Core.Model;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class BiomeClassifierTests
    {
        [Theory]
        [MemberData(nameof(LegacyBoundaryCases))]
        public void Classify_UsesInclusiveLegacyHeightAndSecondaryNoiseBoundaries(
            float terrainHeight, float biomeNoise, string expectedBiome)
        {
            var biome = BiomeClassifier.Classify(terrainHeight, biomeNoise, LegacyRules());

            Assert.Equal(new BiomeId(expectedBiome), biome);
        }

        [Fact]
        public void Classify_ReturnsFirstMatchingExplicitRule()
        {
            var rules = new[]
            {
                new BiomeRule(new BiomeId("First"), 1f, 1f),
                new BiomeRule(new BiomeId("Second"), 1f, 1f)
            };

            var biome = BiomeClassifier.Classify(0.75f, 0.25f, rules);

            Assert.Equal(new BiomeId("First"), biome);
        }

        public static IEnumerable<object[]> LegacyBoundaryCases()
        {
            yield return new object[] { 0.4f, 0.5f, "Deep Water" };
            yield return new object[] { 0.4f, 0.5001f, "Deep Water" };
            yield return new object[] { 0.44f, 0f, "Sand" };
            yield return new object[] { 0.6f, 0.5f, "Forest" };
            yield return new object[] { 0.6f, 0.5001f, "Forest" };
            yield return new object[] { 0.69f, 0.5f, "Jungle" };
            yield return new object[] { 0.69f, 0.5001f, "Jungle" };
            yield return new object[] { 0.8f, 1f, "MountainBase" };
            yield return new object[] { 0.85f, 1f, "MountainMid" };
            yield return new object[] { 0.9f, 1f, "MountainHigh" };
            yield return new object[] { 1f, 1f, "MountainPeak" };
        }

        private static IReadOnlyList<BiomeRule> LegacyRules() => new[]
        {
            new BiomeRule(new BiomeId("Water"), 0.4f, 0.5f),
            new BiomeRule(new BiomeId("Deep Water"), 0.4f, 1f),
            new BiomeRule(new BiomeId("Sand"), 0.44f, 1f),
            new BiomeRule(new BiomeId("Grassland"), 0.6f, 0.5f),
            new BiomeRule(new BiomeId("Forest"), 0.6f, 1f),
            new BiomeRule(new BiomeId("Forest"), 0.69f, 0.5f),
            new BiomeRule(new BiomeId("Jungle"), 0.69f, 1f),
            new BiomeRule(new BiomeId("MountainBase"), 0.8f, 1f),
            new BiomeRule(new BiomeId("MountainMid"), 0.85f, 1f),
            new BiomeRule(new BiomeId("MountainHigh"), 0.9f, 1f),
            new BiomeRule(new BiomeId("MountainPeak"), 1f, 1f)
        };
    }
}
