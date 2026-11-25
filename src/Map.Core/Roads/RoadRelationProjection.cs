using System;
using System.Collections.Generic;
using BaseClasses.Enum;
using BaseClasses.Interface;
using BaseClasses.Model;
using BaseClasses.Model.Params;
using Map.Core.Model;
using Map.Core.Validation;

namespace Map.Core.Roads
{
    /// <summary>
    /// Creates and selects story relations that represent generated roads.
    /// </summary>
    public static class RoadRelationProjection
    {
        /// <summary>
        /// Creates the story relation that represents the supplied road.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="road">The road value.</param>
        /// <param name="mapId">The identifier of the map.</param>
        /// <returns>The story relation that represents the supplied road.</returns>
        public static Relation Create(Plot plot, Road road, Guid mapId)
        {
            var relation = Build(plot, road, mapId);
            plot.Relations.Add(relation);
            return relation;
        }

        /// <summary>
        /// Selects roads whose endpoints both correspond to story elements.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="roads">The roads value.</param>
        /// <returns>The resulting collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="MapValidationException">Thrown when the map snapshot or projection is invalid.</exception>
        internal static IReadOnlyList<Road> SelectStoryRoads(Plot plot, IReadOnlyList<Road> roads)
        {
            if (plot == null) throw new ArgumentNullException(nameof(plot));
            if (roads == null) throw new ArgumentNullException(nameof(roads));
            var elements = FindElements(plot.Elements);
            var roadIds = new HashSet<Guid>();
            var storyRoads = new List<Road>();
            foreach (var road in roads)
            {
                if (road == null || !roadIds.Add(road.RoadId))
                    throw new MapValidationException("Road IDs must be unique.");
                if (elements.TryGetValue(road.FirstEndpointId, out var first) && first.Type == ElemType.Location &&
                    elements.TryGetValue(road.SecondEndpointId, out var second) && second.Type == ElemType.Location)
                    storyRoads.Add(road);
            }
            return storyRoads;
        }

        /// <summary>
        /// Builds a topology plan from the supplied generation context and options.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="road">The road value.</param>
        /// <param name="mapId">The identifier of the map.</param>
        /// <returns>The relation that projects the road into the story graph.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the operation fails with <see cref="KeyNotFoundException"/>.</exception>
        internal static Relation Build(Plot plot, Road road, Guid mapId)
        {
            if (plot == null) throw new ArgumentNullException(nameof(plot));
            if (road == null) throw new ArgumentNullException(nameof(road));
            if (mapId == Guid.Empty) throw new ArgumentException("Map ID cannot be empty.", nameof(mapId));

            var elements = FindElements(plot.Elements);
            if (road.RoadId == Guid.Empty || road.FirstEndpointId == Guid.Empty || road.SecondEndpointId == Guid.Empty)
                throw new ArgumentException("Road and endpoint IDs cannot be empty.", nameof(road));
            if (!elements.TryGetValue(road.FirstEndpointId, out var first))
                throw new KeyNotFoundException($"Road endpoint {road.FirstEndpointId} is not part of the plot.");
            if (!elements.TryGetValue(road.SecondEndpointId, out var second))
                throw new KeyNotFoundException($"Road endpoint {road.SecondEndpointId} is not part of the plot.");
            if (first.Type != ElemType.Location || second.Type != ElemType.Location)
                throw new ArgumentException("Road projections can only connect Location elements.", nameof(road));

            var source = road.FirstEndpointId.CompareTo(road.SecondEndpointId) < 0 ? first : second;
            var target = road.FirstEndpointId.CompareTo(road.SecondEndpointId) < 0 ? second : first;
            return new Relation(source, target,
                new ParamKey<Guid>($"Road/{road.RoadId:D}", $"Map/{mapId:D}"), road.RoadId);
        }

        /// <summary>
        /// Finds elements.
        /// </summary>
        /// <param name="elements">The elements value.</param>
        /// <returns>The elements.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the operation cannot be completed in the current state.</exception>
        private static Dictionary<Guid, IElement> FindElements(IEnumerable<IElement> elements)
        {
            var result = new Dictionary<Guid, IElement>();
            foreach (var element in elements)
                if (!result.TryAdd(element.Id, element))
                    throw new InvalidOperationException("Plot contains duplicate element IDs.");
            return result;
        }
    }
}
