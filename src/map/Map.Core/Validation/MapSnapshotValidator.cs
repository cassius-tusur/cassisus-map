using System;
using System.Collections.Generic;
using Map.Core.Model;

namespace Map.Core.Validation
{
    /// <summary>
    /// Represents an error caused by an invalid map snapshot or projection.
    /// </summary>
    public sealed class MapValidationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MapValidationException"/>.
        /// </summary>
        /// <param name="message">The message value.</param>
        public MapValidationException(string message) : base(message) { }
    }

    /// <summary>
    /// Validates structural and referential invariants of map snapshots.
    /// </summary>
    public static class MapSnapshotValidator
    {
        /// <summary>
        /// Validates the structural consistency of a map snapshot.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="MapValidationException">Thrown when the map snapshot or projection is invalid.</exception>
        public static void Validate(MapSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!MapAlgorithmVersions.IsSupported(snapshot.AlgorithmVersion))
                throw new MapValidationException($"Unsupported map algorithm version '{snapshot.AlgorithmVersion}'.");
            if (snapshot.Dimensions.Width <= 0 || snapshot.Dimensions.Height <= 0)
                throw new MapValidationException("Map dimensions must be positive.");
            var biomeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var biome in snapshot.Biomes)
            {
                if (string.IsNullOrWhiteSpace(biome.Value) || !biomeIds.Add(biome.Value))
                    throw new MapValidationException("Biome definitions must be non-blank and unique.");
            }

            if (snapshot.Cells.Count != (long)snapshot.Dimensions.Width * snapshot.Dimensions.Height)
                throw new MapValidationException("Snapshot must contain exactly one cell per map coordinate.");
            for (var index = 0; index < snapshot.Cells.Count; index++)
            {
                var cell = snapshot.Cells[index];
                var expectedPosition = new GridPosition(index % snapshot.Dimensions.Width,
                    index / snapshot.Dimensions.Width);
                if (cell == null || cell.Position != expectedPosition)
                    throw new MapValidationException("Cells must cover every in-bounds coordinate once in row-major order.");
                if (!biomeIds.Contains(cell.Biome.Value))
                    throw new MapValidationException("Cell references an unknown biome.");
            }

            var featureIds = new HashSet<Guid>();
            var positions = new HashSet<GridPosition>();
            foreach (var placement in snapshot.Placements)
            {
                if (placement == null || !snapshot.Dimensions.Contains(placement.Position))
                    throw new MapValidationException("Placement position is outside map dimensions.");
                if (!featureIds.Add(placement.ElementId))
                    throw new MapValidationException("Map feature IDs must be unique.");
                if (!positions.Add(placement.Position))
                    throw new MapValidationException("Placement positions must be unique.");
                if (!biomeIds.Contains(placement.Biome.Value))
                    throw new MapValidationException("Placement references an unknown biome.");
            }

            var roadIds = new HashSet<Guid>();
            foreach (var road in snapshot.Roads)
            {
                if (road == null || !roadIds.Add(road.RoadId))
                    throw new MapValidationException("Road IDs must be unique.");
                if (!featureIds.Contains(road.FirstEndpointId) || !featureIds.Contains(road.SecondEndpointId))
                    throw new MapValidationException("Road endpoint does not reference a placement.");
                if (road.GridPath.Count == 0)
                    throw new MapValidationException("Snapshot roads must contain a grid path.");
                foreach (var position in road.GridPath)
                    if (!snapshot.Dimensions.Contains(position))
                        throw new MapValidationException("Road path contains an out-of-bounds position.");
            }
        }
    }
}
