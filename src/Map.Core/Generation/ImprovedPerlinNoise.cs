using System;

namespace Map.Core.Generation
{
    /// <summary>
    /// Produces deterministic two-dimensional improved Perlin-noise samples.
    /// </summary>
    public sealed class ImprovedPerlinNoise : INoise2D
    {
        private readonly int[] _permutation = new int[512];

        /// <summary>
        /// Initializes a new instance of <see cref="ImprovedPerlinNoise"/>.
        /// </summary>
        /// <param name="seed">The deterministic seed.</param>
        public ImprovedPerlinNoise(int seed)
        {
            var values = new int[256];
            for (var i = 0; i < values.Length; i++) values[i] = i;

            uint state = unchecked((uint)seed) + 0x9E3779B9u;
            for (var i = values.Length - 1; i > 0; i--)
            {
                state = state * 1664525u + 1013904223u;
                var j = (int)(state % (uint)(i + 1));
                var swap = values[i];
                values[i] = values[j];
                values[j] = swap;
            }

            for (var i = 0; i < _permutation.Length; i++)
                _permutation[i] = values[i & 255];
        }

        /// <summary>
        /// Samples the supplied value.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        public double Sample(double x, double y)
        {
            var xi = FastFloor(x) & 255;
            var yi = FastFloor(y) & 255;
            var xf = x - Math.Floor(x);
            var yf = y - Math.Floor(y);
            var u = Fade(xf);
            var v = Fade(yf);
            var aa = _permutation[_permutation[xi] + yi];
            var ab = _permutation[_permutation[xi] + yi + 1];
            var ba = _permutation[_permutation[xi + 1] + yi];
            var bb = _permutation[_permutation[xi + 1] + yi + 1];
            var value = Lerp(
                Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1d, yf), u),
                Lerp(Grad(ab, xf, yf - 1d), Grad(bb, xf - 1d, yf - 1d), u), v);

            return Math.Max(0d, Math.Min(1d, value * 0.5d + 0.5d));
        }

        /// <summary>
        /// Samples Perlin noise and maps the result to the signed unit interval.
        /// </summary>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        public double SampleSigned(double x, double y) => Sample(x, y) * 2d - 1d;

        /// <summary>
        /// Rounds the value down to the nearest integer.
        /// </summary>
        /// <param name="value">The value to process.</param>
        /// <returns>The computed value.</returns>
        private static int FastFloor(double value) => (int)Math.Floor(value);
        /// <summary>
        /// Applies the Perlin fade curve to the interpolation value.
        /// </summary>
        /// <param name="value">The value to process.</param>
        /// <returns>The computed value.</returns>
        private static double Fade(double value) => value * value * value * (value * (value * 6d - 15d) + 10d);
        /// <summary>
        /// Linearly interpolates between two values.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <param name="amount">The amount value.</param>
        /// <returns>The computed value.</returns>
        private static double Lerp(double left, double right, double amount) => left + amount * (right - left);

        /// <summary>
        /// Calculates the gradient contribution for a Perlin-noise lattice point.
        /// </summary>
        /// <param name="hash">The hash value.</param>
        /// <param name="x">The x value.</param>
        /// <param name="y">The y value.</param>
        /// <returns>The computed value.</returns>
        private static double Grad(int hash, double x, double y)
        {
            switch (hash & 7)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                case 3: return -x - y;
                case 4: return x;
                case 5: return -x;
                case 6: return y;
                default: return -y;
            }
        }
    }
}
