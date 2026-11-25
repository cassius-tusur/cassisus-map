using System;
using System.Collections.Generic;
using Map.Core.Generation;
using Map.Core.Model;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class RoadPathfinderTests
    {
        [Fact]
        public void FindPath_PrefersLowerCostDetourAroundTerrainAbovePreferredMaximumAndIsDeterministic()
        {
            var snapshot = Snapshot(3, 2, new[]
            {
                0f, 1f, 0f,
                0f, 0f, 0f
            });
            var policy = new RoadGenerationPolicy(true, 0.5f, 10f);
            var pathfinder = new RoadPathfinder();

            var first = pathfinder.FindPath(snapshot, new GridPosition(0, 0), new GridPosition(2, 0), policy);
            var second = pathfinder.FindPath(snapshot, new GridPosition(0, 0), new GridPosition(2, 0), policy);

            Assert.Equal(new[]
            {
                new GridPosition(0, 0), new GridPosition(0, 1), new GridPosition(1, 1),
                new GridPosition(2, 1), new GridPosition(2, 0)
            }, first);
            Assert.Equal(first, second);
        }

        [Fact]
        public void FindPath_WhenTheOnlyRouteExceedsPreferredMaximum_RemainsTraversable()
        {
            var snapshot = Snapshot(3, 1, new[] { 0f, 1f, 0f });

            var path = new RoadPathfinder().FindPath(snapshot, new GridPosition(0, 0), new GridPosition(2, 0),
                new RoadGenerationPolicy(true, 0.5f, 1f));

            Assert.Equal(new[]
            {
                new GridPosition(0, 0), new GridPosition(1, 0), new GridPosition(2, 0)
            }, path);
        }

        [Fact]
        public void FindPath_EqualCostRoutes_UsesStableCoordinateTieBreak()
        {
            var snapshot = Snapshot(3, 3, new[] { 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f });
            var pathfinder = new RoadPathfinder();
            var policy = new RoadGenerationPolicy(true, 0.5f, 0f);

            var path = pathfinder.FindPath(snapshot, new GridPosition(0, 1), new GridPosition(2, 1), policy);

            Assert.Equal(new[]
            {
                new GridPosition(0, 1), new GridPosition(0, 0), new GridPosition(1, 0),
                new GridPosition(2, 0), new GridPosition(2, 1)
            }, path);
            Assert.Equal(path, pathfinder.FindPath(snapshot, new GridPosition(0, 1), new GridPosition(2, 1), policy));
        }

        [Theory]
        [InlineData(0.5f)]
        [InlineData(0.5001f)]
        public void FindPath_EndpointAbovePreferredMaximum_RemainsTraversable(float height)
        {
            var snapshot = Snapshot(2, 1, new[] { 0f, height });
            var pathfinder = new RoadPathfinder();
            var policy = new RoadGenerationPolicy(true, 0.5f, 0f);

            var forward = pathfinder.FindPath(snapshot, new GridPosition(0, 0), new GridPosition(1, 0), policy);
            var reverse = pathfinder.FindPath(snapshot, new GridPosition(1, 0), new GridPosition(0, 0), policy);

            Assert.Equal(2, forward.Count);
            Assert.Equal(2, reverse.Count);
        }

        [Fact]
        public void FindPath_SameTraversableCell_ContainsTheEndpoint()
        {
            var snapshot = Snapshot(1, 1, new[] { 0f });

            var path = new RoadPathfinder().FindPath(snapshot, new GridPosition(0, 0), new GridPosition(0, 0),
                new RoadGenerationPolicy(true, 1f, 0f));

            Assert.Equal(new[] { new GridPosition(0, 0) }, path);
        }

        [Fact]
        public void FindPath_OutOfBoundsEndpoints_AreRejected()
        {
            var snapshot = Snapshot(1, 1, new[] { 0f });
            var pathfinder = new RoadPathfinder();
            var policy = new RoadGenerationPolicy(true, 1f, 0f);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                pathfinder.FindPath(snapshot, new GridPosition(-1, 0), new GridPosition(0, 0), policy));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                pathfinder.FindPath(snapshot, new GridPosition(0, 0), new GridPosition(1, 0), policy));
        }

        private static MapSnapshot Snapshot(int width, int height, IReadOnlyList<float> terrainHeights)
        {
            var biome = new BiomeId("plains");
            var cells = new List<MapCell>();
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    cells.Add(new MapCell(new GridPosition(x, y), terrainHeights[y * width + x], biome));

            return new MapSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                new MapDimensions(width, height), 5, "v1", new[] { biome }, cells,
                Array.Empty<LocationPlacement>(), Array.Empty<Road>());
        }
    }
}
