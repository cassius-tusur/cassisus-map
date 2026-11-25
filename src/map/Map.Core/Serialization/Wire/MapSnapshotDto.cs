using System;
using System.Collections.Generic;
using MessagePack;

namespace Map.Core.Serialization.Wire
{
    /// <summary>
    /// Defines the wire representation of a complete map snapshot.
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    internal sealed class MapSnapshotDto
    {
        [Key(0)] public Guid MapId { get; set; }
        [Key(1)] public int Width { get; set; }
        [Key(2)] public int Height { get; set; }
        [Key(3)] public int Seed { get; set; }
        [Key(4)] public string? AlgorithmVersion { get; set; }
        [Key(5)] public List<string>? Biomes { get; set; }
        [Key(6)] public List<MapCellDto>? Cells { get; set; }
        [Key(7)] public List<LocationPlacementDto>? Placements { get; set; }
        [Key(8)] public List<RoadDto>? Roads { get; set; }
    }
}
