using System;
using System.Collections.Generic;
using Map.Core.Model;
using Xunit;

namespace Map.Core.Tests.Model
{
    public class MapValueObjectTests
    {
        [Fact]
        public void BiomeId_RejectsBlankValue()
        {
            Assert.Throws<ArgumentException>(() => new BiomeId("  "));
        }

        [Fact]
        public void Dimensions_RejectsNonPositiveWidth()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MapDimensions(0, 1));
        }

        [Fact]
        public void Dimensions_RejectsNonPositiveHeight()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MapDimensions(1, 0));
        }

        [Fact]
        public void Dimensions_ContainsPositionsWithinExclusiveUpperBounds()
        {
            var dimensions = new MapDimensions(3, 2);

            Assert.True(dimensions.Contains(new GridPosition(0, 0)));
            Assert.True(dimensions.Contains(new GridPosition(2, 1)));
            Assert.False(dimensions.Contains(new GridPosition(3, 1)));
            Assert.False(dimensions.Contains(new GridPosition(2, 2)));
            Assert.False(dimensions.Contains(new GridPosition(-1, 0)));
        }

        [Fact]
        public void MapCell_RejectsTerrainHeightOutsideNormalizedRange()
        {
            var biome = new BiomeId("plains");

            Assert.Throws<ArgumentOutOfRangeException>(() => new MapCell(new GridPosition(0, 0), -0.01f, biome));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MapCell(new GridPosition(0, 0), 1.01f, biome));
        }

        [Fact]
        public void Snapshot_DefensivelyCopiesCompleteFeatureCollections()
        {
            var mapId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var biome = new BiomeId("plains");
            var biomes = new List<BiomeId> { biome };
            var cells = new List<MapCell>
            {
                new MapCell(new GridPosition(0, 0), 0.25f, biome)
            };
            var placements = new List<LocationPlacement>();
            var roads = new List<Road>();

            var snapshot = new MapSnapshot(mapId, new MapDimensions(1, 1), 42, "v1", biomes,
                cells, placements, roads);

            biomes.Clear();
            cells.Clear();
            placements.Add(new LocationPlacement(Guid.NewGuid(), new GridPosition(0, 0), biome));
            roads.Add(new Road(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new[] { new GridPosition(0, 0) }));

            Assert.Equal(new[] { biome }, snapshot.Biomes);
            Assert.Single(snapshot.Cells);
            Assert.Empty(snapshot.Placements);
            Assert.Empty(snapshot.Roads);
        }
    }
}
