using System;

namespace Map.Core.Model
{
    /// <summary>
    /// Describes where a story location is placed in a generated map.
    /// </summary>
    public sealed record LocationPlacement
    {
        public Guid ElementId { get; }
        public GridPosition Position { get; }
        public BiomeId Biome { get; }
        public string Label { get; }
        public BiomeId BiomeId => Biome;

        /// <summary>
        /// Initializes a new instance of <see cref="LocationPlacement"/>.
        /// </summary>
        /// <param name="elementId">The identifier of the element.</param>
        /// <param name="position">The grid position to process.</param>
        /// <param name="biome">The biome identifier.</param>
        /// <param name="label">The label value.</param>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        public LocationPlacement(Guid elementId, GridPosition position, BiomeId biome, string? label = "")
        {
            if (elementId == Guid.Empty) throw new ArgumentException("Element ID cannot be empty.", nameof(elementId));
            ElementId = elementId;
            Position = position;
            Biome = biome;
            Label = label ?? string.Empty;
        }
    }
}
