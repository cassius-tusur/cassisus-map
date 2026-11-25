using System;
using System.Collections.Generic;
using System.Linq;
using BaseClasses.Interface;
using BaseClasses.Model;
using Map.Core.Model;
using Map.Core.Params;
using Map.Core.Roads;
using Map.Core.Validation;

namespace Map.Core.Generation
{
    /// <summary>
    /// Contains a generated snapshot and can project it back onto a plot.
    /// </summary>
    public sealed class MapGenerationResult
    {
        public MapSnapshot Snapshot { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MapGenerationResult"/>.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        public MapGenerationResult(MapSnapshot snapshot)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }

        /// <summary>
        /// Applies to.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="mode">The application or generation mode.</param>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the operation cannot be completed in the current state.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the operation fails with <see cref="KeyNotFoundException"/>.</exception>
        public void ApplyTo(Plot plot, MapApplicationMode mode = MapApplicationMode.RejectConflicts)
        {
            if (plot == null) throw new ArgumentNullException(nameof(plot));
            if (mode != MapApplicationMode.RejectConflicts && mode != MapApplicationMode.ReplaceMap)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown map application mode.");
            MapSnapshotValidator.Validate(Snapshot);
            var keys = MapParams.For(Snapshot.MapId);
            var byId = FindElements(plot.Elements);
            var targets = new List<IElement>(Snapshot.Placements.Count);

            foreach (var placement in Snapshot.Placements)
            {
                if (!byId.TryGetValue(placement.ElementId, out var element))
                    throw new KeyNotFoundException($"Placement element {placement.ElementId} is not part of the plot.");
                if (element.Type != BaseClasses.Enum.ElemType.Location)
                    throw new ArgumentException("Map placements can only target Location elements.", nameof(plot));
                if (mode == MapApplicationMode.RejectConflicts && HasMapNamespaceKey(element, keys.Position.Namespace))
                    throw new InvalidOperationException("The target element already contains map parameters.");
                targets.Add(element);
            }

            var placementIds = new HashSet<Guid>(Snapshot.Placements.Select(placement => placement.ElementId));
            var storyRoads = RoadRelationProjection.SelectStoryRoads(plot, Snapshot.Roads
                .Where(road => placementIds.Contains(road.FirstEndpointId) && placementIds.Contains(road.SecondEndpointId))
                .ToArray());
            var relationsToRemove = RoadProjectionValidator.ValidateForApplication(
                plot, storyRoads, Snapshot.MapId, mode);
            if (mode == MapApplicationMode.ReplaceMap)
            {
                relationsToRemove = plot.Relations
                    .Where(relation => string.Equals(relation.Param.Namespace, keys.Position.Namespace,
                        StringComparison.Ordinal))
                    .ToList();
            }

            if (mode == MapApplicationMode.ReplaceMap)
            {
                foreach (var element in plot.Elements)
                    RemoveMapNamespaceKeys(element, keys.Position.Namespace);
            }

            foreach (var pair in Snapshot.Placements.Zip(targets, (placement, element) => (placement, element)))
            {
                var placement = pair.placement;
                var element = pair.element;
                element.Params.SetOrAdd(keys.Position, placement.Position);
                element.Params.SetOrAdd(keys.Biome, placement.Biome);
                element.Params.SetOrAdd(keys.MapId, Snapshot.MapId);
                element.Params.SetOrAdd(keys.Seed, Snapshot.Seed);
                element.Params.SetOrAdd(keys.AlgorithmVersion, Snapshot.AlgorithmVersion);
            }

            foreach (var relation in relationsToRemove)
                plot.Relations.Remove(relation);
            foreach (var road in storyRoads)
                RoadRelationProjection.Create(plot, road, Snapshot.MapId);
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
            {
                if (!result.TryAdd(element.Id, element))
                    throw new InvalidOperationException("Plot contains duplicate element IDs.");
            }
            return result;
        }

        /// <summary>
        /// Determines whether the element contains a parameter from the map namespace.
        /// </summary>
        /// <param name="element">The element to process.</param>
        /// <param name="namespace">The namespace value.</param>
        /// <returns><see langword="true"/> if the element contains a parameter from the map namespace; otherwise, <see langword="false"/>.</returns>
        private static bool HasMapNamespaceKey(IElement element, string @namespace)
        {
            foreach (var key in element.Params.Keys)
                if (string.Equals(key.Namespace, @namespace, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// Removes all parameters that belong to the specified map namespace.
        /// </summary>
        /// <param name="element">The element to process.</param>
        /// <param name="namespace">The namespace value.</param>
        private static void RemoveMapNamespaceKeys(IElement element, string @namespace)
        {
            foreach (var key in element.Params.Keys
                .Where(key => string.Equals(key.Namespace, @namespace, StringComparison.Ordinal))
                .ToList())
                element.Params.Remove(key);
        }
    }

    /// <summary>
    /// Provides parameter-bag helpers used when projecting generated map data.
    /// </summary>
    internal static class ParamBagExtensions
    {
        /// <summary>
        /// Replaces the value for an existing parameter or adds a new parameter.
        /// </summary>
        /// <typeparam name="T">The type of value processed by the method.</typeparam>
        /// <param name="bag">The bag value.</param>
        /// <param name="key">The parameter key to use.</param>
        /// <param name="value">The value to process.</param>
        public static void SetOrAdd<T>(this BaseClasses.Model.Params.ParamBag bag,
            BaseClasses.Model.Params.ParamKey<T> key, T value)
        {
            if (bag.ContainsKey(key)) bag.Set(key, value);
            else bag.Add(key, value);
        }
    }
}
