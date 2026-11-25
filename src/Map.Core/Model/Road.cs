using System;
using System.Collections.Generic;

namespace Map.Core.Model
{
    /// <summary>
    /// Describes a generated road and its ordered grid path between two locations.
    /// </summary>
    public sealed record Road
    {
        public Guid RoadId { get; }
        public Guid FirstFeatureId { get; }
        public Guid SecondFeatureId { get; }
        public Guid FirstEndpointId => FirstFeatureId;
        public Guid SecondEndpointId => SecondFeatureId;
        // V1 snapshots own the complete geometry; consumers do not restore paths from endpoints.
        public IReadOnlyList<GridPosition> GridPath { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="Road"/>.
        /// </summary>
        /// <param name="roadId">The identifier of the road.</param>
        /// <param name="firstFeatureId">The identifier of the first feature.</param>
        /// <param name="secondFeatureId">The identifier of the second feature.</param>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        public Road(Guid roadId, Guid firstFeatureId, Guid secondFeatureId)
        {
            if (roadId == Guid.Empty) throw new ArgumentException("Road ID cannot be empty.", nameof(roadId));
            if (firstFeatureId == Guid.Empty) throw new ArgumentException("Feature ID cannot be empty.", nameof(firstFeatureId));
            if (secondFeatureId == Guid.Empty) throw new ArgumentException("Feature ID cannot be empty.", nameof(secondFeatureId));

            RoadId = roadId;
            FirstFeatureId = firstFeatureId;
            SecondFeatureId = secondFeatureId;
            GridPath = Array.AsReadOnly(Array.Empty<GridPosition>());
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Road"/>.
        /// </summary>
        /// <param name="roadId">The identifier of the road.</param>
        /// <param name="firstFeatureId">The identifier of the first feature.</param>
        /// <param name="secondFeatureId">The identifier of the second feature.</param>
        /// <param name="gridPath">The grid path value.</param>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        public Road(Guid roadId, Guid firstFeatureId, Guid secondFeatureId, IReadOnlyList<GridPosition> gridPath)
        {
            if (roadId == Guid.Empty) throw new ArgumentException("Road ID cannot be empty.", nameof(roadId));
            if (firstFeatureId == Guid.Empty) throw new ArgumentException("Feature ID cannot be empty.", nameof(firstFeatureId));
            if (secondFeatureId == Guid.Empty) throw new ArgumentException("Feature ID cannot be empty.", nameof(secondFeatureId));
            if (gridPath == null) throw new ArgumentNullException(nameof(gridPath));
            if (gridPath.Count == 0) throw new ArgumentException("Road path cannot be empty.", nameof(gridPath));

            RoadId = roadId;
            FirstFeatureId = firstFeatureId;
            SecondFeatureId = secondFeatureId;
            GridPath = Array.AsReadOnly(new List<GridPosition>(gridPath).ToArray());
        }
    }
}
