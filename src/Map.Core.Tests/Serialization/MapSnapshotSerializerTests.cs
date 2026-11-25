using System;
using System.Reflection;
using Map.Core.Model;
using Map.Core.Serialization;
using MessagePack;
using Xunit;

namespace Map.Core.Tests.Serialization
{
    public class MapSnapshotSerializerTests
    {
        [Fact]
        public void Serializer_UsesUntrustedMessagePackSecurityOptions()
        {
            var optionsField = typeof(MapSnapshotSerializer).GetField("SerializerOptions",
                BindingFlags.NonPublic | BindingFlags.Static);

            var options = Assert.IsType<MessagePackSerializerOptions>(optionsField?.GetValue(null));

            Assert.True(options.Security.HashCollisionResistant);
            Assert.True(options.Security.MaximumObjectGraphDepth <=
                MessagePackSecurity.UntrustedData.MaximumObjectGraphDepth);
        }

        [Fact]
        public void SnapshotBinary_RoundTripPreservesCellsPlacementLabelsAndRoads()
        {
            var first = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var second = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var snapshot = new MapSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                new MapDimensions(3, 2), 42, "v1", new[] { new BiomeId("coast") },
                new[]
                {
                    new MapCell(new GridPosition(0, 0), 0.1f, new BiomeId("coast")),
                    new MapCell(new GridPosition(1, 0), 0.2f, new BiomeId("coast")),
                    new MapCell(new GridPosition(2, 0), 0.3f, new BiomeId("coast")),
                    new MapCell(new GridPosition(0, 1), 0.4f, new BiomeId("coast")),
                    new MapCell(new GridPosition(1, 1), 0.5f, new BiomeId("coast")),
                    new MapCell(new GridPosition(2, 1), 0.6f, new BiomeId("coast"))
                }, new[]
                {
                    new LocationPlacement(first, new GridPosition(0, 0), new BiomeId("coast"), "Harbor"),
                    new LocationPlacement(second, new GridPosition(1, 0), new BiomeId("coast"), "Watchtower")
                },
                new[] { new Road(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), first, second,
                    new[] { new GridPosition(0, 0), new GridPosition(1, 0) }) });
            var serializer = new MapSnapshotSerializer();

            var payload = serializer.Serialize(snapshot);
            Assert.IsType<byte[]>(payload);
            var restored = serializer.Deserialize(payload);

            Assert.Equal(snapshot.MapId, restored.MapId);
            Assert.Equal(snapshot.Dimensions, restored.Dimensions);
            Assert.Equal(snapshot.Seed, restored.Seed);
            Assert.Equal(snapshot.AlgorithmVersion, restored.AlgorithmVersion);
            Assert.Equal(snapshot.Biomes, restored.Biomes);
            Assert.Equal(snapshot.Cells, restored.Cells);
            Assert.Equal(snapshot.Placements, restored.Placements);
            Assert.Equal("Harbor", restored.Placements[0].Label);
            Assert.Equal("Watchtower", restored.Placements[1].Label);
            Assert.Equal(snapshot.Roads.Count, restored.Roads.Count);
            Assert.Equal(snapshot.Roads[0].RoadId, restored.Roads[0].RoadId);
            Assert.Equal(snapshot.Roads[0].FirstEndpointId, restored.Roads[0].FirstEndpointId);
            Assert.Equal(snapshot.Roads[0].SecondEndpointId, restored.Roads[0].SecondEndpointId);
            Assert.Equal(snapshot.Roads[0].GridPath, restored.Roads[0].GridPath);
        }
    }
}
