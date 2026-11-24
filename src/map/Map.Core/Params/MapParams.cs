using System;

namespace Map.Core.Params
{
    /// <summary>
    /// Creates parameter-key sets scoped to a generated map identifier.
    /// </summary>
    public static class MapParams
    {
        /// <summary>
        /// Creates the parameter set for the specified map.
        /// </summary>
        /// <param name="mapId">The identifier of the map.</param>
        /// <returns>The parameter set for the specified map.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        public static MapParamSet For(Guid mapId)
        {
            if (mapId == Guid.Empty)
                throw new ArgumentException("Map ID cannot be empty.", nameof(mapId));

            return new MapParamSet(mapId, $"Map/{mapId:D}");
        }
    }
}
