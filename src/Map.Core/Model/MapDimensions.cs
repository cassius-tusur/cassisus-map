using System;

namespace Map.Core.Model
{
    /// <summary>
    /// Defines the width and height of a rectangular map grid.
    /// </summary>
    public readonly record struct MapDimensions
    {
        public int Width { get; }
        public int Height { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MapDimensions"/>.
        /// </summary>
        /// <param name="width">The width value.</param>
        /// <param name="height">The height value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public MapDimensions(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");

            Width = width;
            Height = height;
        }

        /// <summary>
        /// Determines whether the collection contains the specified value.
        /// </summary>
        /// <param name="position">The grid position to process.</param>
        /// <returns><see langword="true"/> if the collection contains the specified value; otherwise, <see langword="false"/>.</returns>
        public bool Contains(GridPosition position) =>
            position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;
    }
}
