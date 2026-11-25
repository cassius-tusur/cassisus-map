using MessagePack;

namespace Map.Core.Model
{
    /// <summary>
    /// Represents an integer position in the map grid.
    /// </summary>
    /// <param name="X">The zero-based horizontal coordinate.</param>
    /// <param name="Y">The zero-based vertical coordinate.</param>
    /// <seealso cref="MapDimensions"/>
    [MessagePackObject]
    public readonly record struct GridPosition([property: Key(0)] int X, [property: Key(1)] int Y);
}
