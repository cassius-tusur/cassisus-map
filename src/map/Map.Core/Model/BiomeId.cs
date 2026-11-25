using System;
using MessagePack;

namespace Map.Core.Model
{
    /// <summary>
    /// Identifies a biome by its stable string value.
    /// </summary>
    [MessagePackObject]
    public readonly record struct BiomeId
    {
        [Key(0)] public string Value { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="BiomeId"/>.
        /// </summary>
        /// <param name="value">The value to process.</param>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        public BiomeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Biome ID cannot be null or whitespace.", nameof(value));

            Value = value;
        }
    }
}
