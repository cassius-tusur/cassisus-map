using MessagePack;

namespace Map.Core.Serialization.Wire
{
    /// <summary>
    /// Wraps a map snapshot DTO with its wire-format version.
    /// </summary>
    [MessagePackObject(AllowPrivate = true)]
    internal sealed class MapSnapshotEnvelopeDto
    {
        [Key(0)] public int FormatVersion { get; set; }
        [Key(1)] public MapSnapshotDto? Snapshot { get; set; }
    }
}
