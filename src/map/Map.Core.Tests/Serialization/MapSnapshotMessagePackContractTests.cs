using System;
using System.Collections.Generic;
using System.Linq;
using Map.Core.Model;
using Map.Core.Serialization;
using Map.Core.Validation;
using MessagePack;
using Xunit;

namespace Map.Core.Tests.Serialization
{
    public class MapSnapshotMessagePackContractTests
    {
        [Fact]
        public void Serialize_WritesV1Envelope()
        {
            var payload = new MapSnapshotSerializer().Serialize(Snapshot());

            var envelope = MessagePackSerializer.Deserialize<EnvelopeContract>(payload);

            Assert.Equal(1, envelope.FormatVersion);
            Assert.NotNull(envelope.Snapshot);
        }

        [Fact]
        public void Deserialize_RejectsEmptyOrCorruptPayload()
        {
            var serializer = new MapSnapshotSerializer();

            Assert.Throws<MapValidationException>(() => serializer.Deserialize(Array.Empty<byte>()));
            Assert.Throws<MapValidationException>(() => serializer.Deserialize(new byte[] { 0xc1 }));
        }

        [Fact]
        public void Deserialize_RejectsPayloadWithTrailingBytes()
        {
            var serializer = new MapSnapshotSerializer();
            var payload = serializer.Serialize(Snapshot());
            var payloadWithTrailingByte = new byte[payload.Length + 1];
            Buffer.BlockCopy(payload, 0, payloadWithTrailingByte, 0, payload.Length);
            payloadWithTrailingByte[payload.Length] = 0xc1;

            var error = Assert.Throws<MapValidationException>(() => serializer.Deserialize(payloadWithTrailingByte));

            Assert.Contains("trailing", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Deserialize_RejectsUnknownFormatVersion()
        {
            var payload = SerializeContract(new EnvelopeContract { FormatVersion = 2, Snapshot = ToContract(Snapshot()) });

            var error = Assert.Throws<MapValidationException>(() => new MapSnapshotSerializer().Deserialize(payload));

            Assert.Contains("format version", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Deserialize_RejectsUnknownAlgorithmVersion()
        {
            var contract = ToContract(Snapshot());
            contract.AlgorithmVersion = "v2";
            var payload = SerializeContract(new EnvelopeContract { FormatVersion = 1, Snapshot = contract });

            var error = Assert.Throws<MapValidationException>(() => new MapSnapshotSerializer().Deserialize(payload));

            Assert.Contains("algorithm version", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Deserialize_RejectsMalformedCellGrid()
        {
            var contract = ToContract(Snapshot());
            contract.Cells.RemoveAt(contract.Cells.Count - 1);
            var payload = SerializeContract(new EnvelopeContract { FormatVersion = 1, Snapshot = contract });

            var error = Assert.Throws<MapValidationException>(() => new MapSnapshotSerializer().Deserialize(payload));

            Assert.Contains("cell", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] SerializeContract(EnvelopeContract envelope) => MessagePackSerializer.Serialize(envelope);

        private static EnvelopeContractSnapshot ToContract(MapSnapshot snapshot) => new EnvelopeContractSnapshot
        {
            MapId = snapshot.MapId,
            Width = snapshot.Dimensions.Width,
            Height = snapshot.Dimensions.Height,
            Seed = snapshot.Seed,
            AlgorithmVersion = snapshot.AlgorithmVersion,
            Biomes = snapshot.Biomes.Select(biome => biome.Value).ToList(),
            Cells = snapshot.Cells.Select(cell => new CellContract
            {
                X = cell.Position.X, Y = cell.Position.Y, TerrainHeight = cell.TerrainHeight, Biome = cell.Biome.Value
            }).ToList(),
            Placements = snapshot.Placements.Select(placement => new PlacementContract
            {
                ElementId = placement.ElementId, X = placement.Position.X, Y = placement.Position.Y,
                Biome = placement.Biome.Value, Label = placement.Label
            }).ToList(),
            Roads = snapshot.Roads.Select(road => new RoadContract
            {
                RoadId = road.RoadId, FirstEndpointId = road.FirstEndpointId, SecondEndpointId = road.SecondEndpointId,
                Path = road.GridPath.Select(position => new PositionContract { X = position.X, Y = position.Y }).ToList()
            }).ToList()
        };

        private static MapSnapshot Snapshot()
        {
            var first = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var second = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            return new MapSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"), new MapDimensions(2, 2),
                42, "v1", new[] { new BiomeId("coast") }, new[]
                {
                    new MapCell(new GridPosition(0, 0), 0.1f, new BiomeId("coast")),
                    new MapCell(new GridPosition(1, 0), 0.2f, new BiomeId("coast")),
                    new MapCell(new GridPosition(0, 1), 0.3f, new BiomeId("coast")),
                    new MapCell(new GridPosition(1, 1), 0.4f, new BiomeId("coast"))
                }, new[]
                {
                    new LocationPlacement(first, new GridPosition(0, 0), new BiomeId("coast")),
                    new LocationPlacement(second, new GridPosition(1, 0), new BiomeId("coast"))
                }, new[] { new Road(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), first, second,
                    new[] { new GridPosition(0, 0), new GridPosition(1, 0) }) });
        }

        [MessagePackObject(AllowPrivate = true)]
        internal sealed class EnvelopeContract
        {
            [Key(0)] public int FormatVersion { get; set; }
            [Key(1)] public EnvelopeContractSnapshot? Snapshot { get; set; }
        }

        [MessagePackObject(AllowPrivate = true)]
        internal sealed class EnvelopeContractSnapshot
        {
            [Key(0)] public Guid MapId { get; set; }
            [Key(1)] public int Width { get; set; }
            [Key(2)] public int Height { get; set; }
            [Key(3)] public int Seed { get; set; }
            [Key(4)] public string AlgorithmVersion { get; set; } = "";
            [Key(5)] public List<string> Biomes { get; set; } = new List<string>();
            [Key(6)] public List<CellContract> Cells { get; set; } = new List<CellContract>();
            [Key(7)] public List<PlacementContract> Placements { get; set; } = new List<PlacementContract>();
            [Key(8)] public List<RoadContract> Roads { get; set; } = new List<RoadContract>();
        }

        [MessagePackObject(AllowPrivate = true)]
        internal sealed class CellContract
        {
            [Key(0)] public int X { get; set; }
            [Key(1)] public int Y { get; set; }
            [Key(2)] public float TerrainHeight { get; set; }
            [Key(3)] public string Biome { get; set; } = "";
        }

        [MessagePackObject(AllowPrivate = true)]
        internal sealed class PlacementContract
        {
            [Key(0)] public Guid ElementId { get; set; }
            [Key(1)] public int X { get; set; }
            [Key(2)] public int Y { get; set; }
            [Key(3)] public string Biome { get; set; } = "";
            [Key(4)] public string Label { get; set; } = "";
        }

        [MessagePackObject(AllowPrivate = true)]
        internal sealed class RoadContract
        {
            [Key(0)] public Guid RoadId { get; set; }
            [Key(1)] public Guid FirstEndpointId { get; set; }
            [Key(2)] public Guid SecondEndpointId { get; set; }
            [Key(3)] public List<PositionContract> Path { get; set; } = new List<PositionContract>();
        }

        [MessagePackObject(AllowPrivate = true)]
        internal sealed class PositionContract
        {
            [Key(0)] public int X { get; set; }
            [Key(1)] public int Y { get; set; }
        }
    }
}
