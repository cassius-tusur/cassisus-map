using System;
using BaseClasses.Model.Params;
using Map.Core.Model;

namespace Map.Core.Params
{
    /// <summary>
    /// Contains the map-specific parameter keys used to project generation data.
    /// </summary>
    public sealed class MapParamSet
    {
        public ParamKey<GridPosition> Position { get; }
        public ParamKey<BiomeId> Biome { get; }
        public ParamKey<Guid> MapId { get; }
        public ParamKey<int> Seed { get; }
        public ParamKey<string> AlgorithmVersion { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MapParamSet"/>.
        /// </summary>
        /// <param name="mapId">The identifier of the map.</param>
        /// <param name="namespace">The namespace value.</param>
        internal MapParamSet(Guid mapId, string @namespace)
        {
            Position = new ParamKey<GridPosition>(nameof(Position), @namespace);
            Biome = new ParamKey<BiomeId>(nameof(Biome), @namespace);
            MapId = new ParamKey<Guid>(nameof(MapId), @namespace);
            Seed = new ParamKey<int>(nameof(Seed), @namespace);
            AlgorithmVersion = new ParamKey<string>(nameof(AlgorithmVersion), @namespace);
        }
    }
}
