using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Map.Core.Generation
{
    /// <summary>
    /// Small deterministic LCG used for map-generation choices. It deliberately has no dependency on
    /// framework random-number implementations so a v1 seed has the same meaning on every target.
    /// </summary>
    internal sealed class DeterministicRandom
    {
        private uint _state;

        /// <summary>
        /// Initializes a new instance of <see cref="DeterministicRandom"/>.
        /// </summary>
        /// <param name="seed">The deterministic seed.</param>
        public DeterministicRandom(int seed)
        {
            _state = unchecked((uint)seed + 0x9E3779B9u);
        }

        /// <summary>
        /// Returns the next deterministic unsigned integer.
        /// </summary>
        /// <returns>The computed value.</returns>
        public uint NextUInt()
        {
            _state = unchecked(_state * 1664525u + 1013904223u);
            return _state;
        }

        /// <summary>
        /// Returns the next deterministic floating-point value in the unit interval.
        /// </summary>
        /// <returns>The computed value.</returns>
        public double NextUnitDouble() => NextUInt() / ((double)uint.MaxValue + 1d);
        /// <summary>
        /// Shuffles the supplied list using the deterministic random sequence.
        /// </summary>
        /// <typeparam name="T">The type of value processed by the method.</typeparam>
        /// <param name="values">The values to process.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>

        public void Shuffle<T>(IList<T> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));

            for (var index = values.Count - 1; index > 0; index--)
            {
                var swapIndex = (int)(NextUInt() % (uint)(index + 1));
                var value = values[index];
                values[index] = values[swapIndex];
                values[swapIndex] = value;
            }
        }

        /// <summary>
        /// Creates a deterministic identifier from the map, category, and stable input.
        /// </summary>
        /// <param name="mapId">The identifier of the map.</param>
        /// <param name="category">The category value.</param>
        /// <param name="stableInput">The stable input value.</param>
        /// <returns>The resulting identifier.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        public static Guid CreateStableGuid(Guid mapId, string category, string stableInput)
        {
            if (mapId == Guid.Empty) throw new ArgumentException("Map ID cannot be empty.", nameof(mapId));
            if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category cannot be blank.", nameof(category));
            if (stableInput == null) throw new ArgumentNullException(nameof(stableInput));

            var input = Encoding.UTF8.GetBytes($"{mapId:D}|{category}|{stableInput}");
            using var hash = MD5.Create();
            return new Guid(hash.ComputeHash(input));
        }
    }
}
