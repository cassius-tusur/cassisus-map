using System;
using System.Collections.Generic;
using System.Linq;
using BaseClasses.Model;
using BaseClasses.Model.Params;
using Map.Core.Generation;
using Map.Core.Model;
using Map.Core.Validation;

namespace Map.Core.Roads
{
    /// <summary>
    /// Validates projected road relations against generated roads.
    /// </summary>
    public static class RoadProjectionValidator
    {
        /// <summary>
        /// Validates that projected road relations match the supplied roads.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="roads">The roads value.</param>
        /// <param name="mapId">The identifier of the map.</param>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="MapValidationException">Thrown when the map snapshot or projection is invalid.</exception>
        public static void Validate(Plot plot, IReadOnlyList<Road> roads, Guid mapId)
        {
            if (plot == null) throw new ArgumentNullException(nameof(plot));
            if (roads == null) throw new ArgumentNullException(nameof(roads));
            if (mapId == Guid.Empty) throw new ArgumentException("Map ID cannot be empty.", nameof(mapId));

            foreach (var road in RoadRelationProjection.SelectStoryRoads(plot, roads))
            {
                var expected = RoadRelationProjection.Build(plot, road, mapId);
                var matches = plot.Relations.Count(relation => IsMatch(relation, expected));
                if (matches != 1)
                    throw new MapValidationException("Each story road must have exactly one canonical projection.");
            }
        }

        /// <summary>
        /// Validates for application.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="roads">The roads value.</param>
        /// <param name="mapId">The identifier of the map.</param>
        /// <param name="mode">The application or generation mode.</param>
        /// <returns>The resulting collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the operation cannot be completed in the current state.</exception>
        internal static List<Relation> ValidateForApplication(Plot plot, IReadOnlyList<Road> roads, Guid mapId,
            MapApplicationMode mode)
        {
            if (roads == null) throw new ArgumentNullException(nameof(roads));
            var removals = new List<Relation>();
            foreach (var road in roads)
            {
                var expected = RoadRelationProjection.Build(plot, road, mapId);
                var sameKey = plot.Relations.Where(relation => relation.Param.Equals(expected.Param)).ToList();
                if (mode == MapApplicationMode.RejectConflicts && sameKey.Count != 0)
                    throw new InvalidOperationException("The plot already contains a projection for a map road.");
                removals.AddRange(sameKey);
            }
            return removals;
        }

        /// <summary>
        /// Determines whether the actual relation matches the expected relation.
        /// </summary>
        /// <param name="actual">The actual value.</param>
        /// <param name="expected">The expected value.</param>
        /// <returns><see langword="true"/> if the actual relation matches the expected relation; otherwise, <see langword="false"/>.</returns>
        private static bool IsMatch(Relation actual, Relation expected)
        {
            return actual.Source.Id == expected.Source.Id && actual.Target.Id == expected.Target.Id &&
                   actual.Param.Equals(expected.Param) && actual.Value is Guid value &&
                   expected.Value is Guid expectedValue && value == expectedValue;
        }
    }
}
