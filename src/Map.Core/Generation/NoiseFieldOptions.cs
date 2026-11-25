using System;

namespace Map.Core.Generation
{
    /// <summary>
    /// Defines dimensions and octave parameters for fractal-noise generation.
    /// </summary>
    public sealed record NoiseFieldOptions
    {
        public float Scale { get; }
        public int Octaves { get; }
        public float Persistence { get; }
        public float Lacunarity { get; }
        public float OffsetX { get; }
        public float OffsetY { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="NoiseFieldOptions"/>.
        /// </summary>
        /// <param name="scale">The scale value.</param>
        /// <param name="octaves">The octaves value.</param>
        /// <param name="persistence">The persistence value.</param>
        /// <param name="lacunarity">The lacunarity value.</param>
        /// <param name="offsetX">The offset x value.</param>
        /// <param name="offsetY">The offset y value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public NoiseFieldOptions(float scale, int octaves, float persistence, float lacunarity,
            float offsetX, float offsetY)
        {
            if (!IsPositiveFinite(scale))
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be positive and finite.");
            if (octaves <= 0)
                throw new ArgumentOutOfRangeException(nameof(octaves), octaves, "Octaves must be positive.");
            if (!IsPositiveFinite(persistence))
                throw new ArgumentOutOfRangeException(nameof(persistence), persistence,
                    "Persistence must be positive and finite.");
            if (!IsPositiveFinite(lacunarity))
                throw new ArgumentOutOfRangeException(nameof(lacunarity), lacunarity,
                    "Lacunarity must be positive and finite.");
            if (!IsFinite(offsetX))
                throw new ArgumentOutOfRangeException(nameof(offsetX), offsetX, "Offset X must be finite.");
            if (!IsFinite(offsetY))
                throw new ArgumentOutOfRangeException(nameof(offsetY), offsetY, "Offset Y must be finite.");

            Scale = scale;
            Octaves = octaves;
            Persistence = persistence;
            Lacunarity = lacunarity;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }

        /// <summary>
        /// Determines whether the value is positive and finite.
        /// </summary>
        /// <param name="value">The value to process.</param>
        /// <returns><see langword="true"/> if the value is positive and finite; otherwise, <see langword="false"/>.</returns>
        private static bool IsPositiveFinite(float value) => value > 0f && IsFinite(value);
        /// <summary>
        /// Determines whether the value is finite.
        /// </summary>
        /// <param name="value">The value to process.</param>
        /// <returns><see langword="true"/> if the value is finite; otherwise, <see langword="false"/>.</returns>
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
