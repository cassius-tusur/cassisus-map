using System;
using Map.Core.Model;
using Map.Core.Serialization;
using Xunit;

namespace Map.Core.Tests.Roads
{
    public class RoadGeometryRoundTripTests
    {
        [Theory]
        [InlineData(64, 64)]
        [InlineData(256, 256)]
        public void StoredGeometry_RoundTripsIdentically(int width, int height)
        {
            var first = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var second = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var road = new Road(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), first, second,
                new[]
                {
                    new GridPosition(0, 0),
                    new GridPosition(width / 2, height / 2),
                    new GridPosition(width - 1, height - 1)
                });
            var snapshot = new MapSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                new MapDimensions(width, height), 42, "v1", new[] { new BiomeId("plains") },
                new[]
                {
                    new LocationPlacement(first, new GridPosition(0, 0), new BiomeId("plains")),
                    new LocationPlacement(second, new GridPosition(width - 1, height - 1), new BiomeId("plains"))
                }, new[] { road });

            var restored = new MapSnapshotSerializer().Deserialize(
                new MapSnapshotSerializer().Serialize(snapshot));

            Assert.Equal(road.GridPath, restored.Roads[0].GridPath);
        }

    }
}
