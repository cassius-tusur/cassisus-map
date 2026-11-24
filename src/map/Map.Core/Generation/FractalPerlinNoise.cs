using System;
using Map.Core.Model;

namespace Map.Core.Generation
{
    /// <summary>
    /// Generates normalized fractal Perlin-noise fields from deterministic options.
    /// </summary>
    public static class FractalPerlinNoise
    {
        /// <summary>
        /// Generates a normalized fractal Perlin-noise field.
        /// </summary>
        /// <param name="dimensions">The map dimensions.</param>
        /// <param name="seed">The deterministic seed.</param>
        /// <param name="options">The generation options.</param>
        /// <returns>The normalized noise field in row-major order.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        public static float[] Generate(MapDimensions dimensions, int seed, NoiseFieldOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            var cellCount = checked(dimensions.Width * dimensions.Height);
            var values = new double[cellCount];
            var octaveOffsets = CreateOctaveOffsets(seed, options);
            var sampler = new ImprovedPerlinNoise(seed);
            var minimum = double.MaxValue;
            var maximum = double.MinValue;
            var halfWidth = dimensions.Width / 2d;
            var halfHeight = dimensions.Height / 2d;

            for (var y = 0; y < dimensions.Height; y++)
            {
                for (var x = 0; x < dimensions.Width; x++)
                {
                    var amplitude = 1d;
                    var frequency = 1d;
                    var height = 0d;

                    for (var octave = 0; octave < octaveOffsets.Length; octave++)
                    {
                        var sampleX = (x - halfWidth) / options.Scale * frequency + octaveOffsets[octave].X;
                        var sampleY = (y - halfHeight) / options.Scale * frequency + octaveOffsets[octave].Y;
                        height += sampler.SampleSigned(sampleX, sampleY) * amplitude;
                        amplitude *= options.Persistence;
                        frequency *= options.Lacunarity;
                    }

                    var index = y * dimensions.Width + x;
                    values[index] = height;
                    if (height < minimum) minimum = height;
                    if (height > maximum) maximum = height;
                }
            }

            var normalized = new float[cellCount];
            var range = maximum - minimum;
            if (range == 0d) return normalized;

            for (var index = 0; index < values.Length; index++)
                normalized[index] = (float)Math.Max(0d, Math.Min(1d, (values[index] - minimum) / range));

            return normalized;
        }

        /// <summary>
        /// Creates deterministic offsets for each noise octave.
        /// </summary>
        /// <param name="seed">The deterministic seed.</param>
        /// <param name="options">The generation options.</param>
        /// <returns>The deterministic offsets for each noise octave.</returns>
        private static OctaveOffset[] CreateOctaveOffsets(int seed, NoiseFieldOptions options)
        {
            var random = new DeterministicOffsetRandom(seed);
            var offsets = new OctaveOffset[options.Octaves];
            for (var index = 0; index < offsets.Length; index++)
            {
                offsets[index] = new OctaveOffset(
                    random.Next(-100000, 100000) + options.OffsetX,
                    random.Next(-100000, 100000) + options.OffsetY);
            }

            return offsets;
        }

        /// <summary>
        /// Stores the deterministic coordinate offset for one noise octave.
        /// </summary>
        private readonly struct OctaveOffset
        {
            public double X { get; }
            public double Y { get; }

            /// <summary>
            /// Initializes a new instance of <see cref="OctaveOffset"/>.
            /// </summary>
            /// <param name="x">The x value.</param>
            /// <param name="y">The y value.</param>
            public OctaveOffset(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        /// <summary>
        /// Generates deterministic offsets for fractal-noise octaves.
        /// </summary>
        private struct DeterministicOffsetRandom
        {
            private uint _state;

            /// <summary>
            /// Initializes a new instance of <see cref="DeterministicOffsetRandom"/>.
            /// </summary>
            /// <param name="seed">The deterministic seed.</param>
            public DeterministicOffsetRandom(int seed)
            {
                _state = unchecked((uint)seed) + 0x9E3779B9u;
            }

            /// <summary>
            /// Returns the next pseudo-random value in the requested range.
            /// </summary>
            /// <param name="minimumInclusive">The inclusive lower bound.</param>
            /// <param name="maximumExclusive">The exclusive upper bound.</param>
            /// <returns>The computed value.</returns>
            public int Next(int minimumInclusive, int maximumExclusive)
            {
                _state = _state * 1664525u + 1013904223u;
                return minimumInclusive + (int)(_state % (uint)(maximumExclusive - minimumInclusive));
            }
        }
    }
}
