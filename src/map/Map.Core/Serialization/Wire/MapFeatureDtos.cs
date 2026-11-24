using System;
using System.Collections.Generic;
using MessagePack;

namespace Map.Core.Serialization.Wire
{
    /// <summary>
    /// Defines the wire representation of a generated map cell.
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    internal sealed class MapCellDto
    {
        [Key(0)] public int X { get; set; }
        [Key(1)] public int Y { get; set; }
        [Key(2)] public float TerrainHeight { get; set; }
        [Key(3)] public string? Biome { get; set; }
    }

    /// <summary>
    /// Defines the wire representation of a location placement.
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    internal sealed class LocationPlacementDto
    {
        [Key(0)] public Guid ElementId { get; set; }
        [Key(1)] public int X { get; set; }
        [Key(2)] public int Y { get; set; }
        [Key(3)] public string? Biome { get; set; }
        [Key(4)] public string? Label { get; set; }
    }

    /// <summary>
    /// Defines the wire representation of a generated road.
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    internal sealed class RoadDto
    {
        [Key(0)] public Guid RoadId { get; set; }
        [Key(1)] public Guid FirstEndpointId { get; set; }
        [Key(2)] public Guid SecondEndpointId { get; set; }
        [Key(3)] public List<GridPositionDto>? Path { get; set; }
    }

    /// <summary>
    /// Defines the wire representation of a grid position.
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    internal sealed class GridPositionDto
    {
        [Key(0)] public int X { get; set; }
        [Key(1)] public int Y { get; set; }
    }
}
