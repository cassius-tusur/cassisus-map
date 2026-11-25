using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BaseClasses.Enum;
using BaseClasses.Interface;
using BaseClasses.Model;
using Map.Core.Model;
using Map.Core.Params;

namespace Map.Core.Generation
{
    /// <summary>
    /// Projects generated map placement data onto existing location elements.
    /// </summary>
    public static class MapLocationService
    {
        /// <summary>
        /// Places an existing plot element into the supplied map snapshot.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="element">The element to process.</param>
        /// <returns>A generation result containing the updated map snapshot.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the operation cannot be completed in the current state.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when the operation fails with <see cref="KeyNotFoundException"/>.</exception>
        public static MapGenerationResult PlaceExistingLocation(Plot plot, MapSnapshot snapshot, IElement element)
        {
            if (plot == null) throw new ArgumentNullException(nameof(plot));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Type != ElemType.Location)
                throw new ArgumentException("Only Location elements can be placed.", nameof(element));
            if (Array.FindIndex(snapshot.Placements.ToArray(), p => p.ElementId == element.Id) < 0)
                throw new ArgumentException("The snapshot does not contain a placement for this element.", nameof(element));
            var matches = plot.Elements.Count(candidate => candidate.Id == element.Id);
            if (matches == 0)
                throw new KeyNotFoundException($"Location {element.Id} is not part of the plot.");
            if (matches > 1)
                throw new InvalidOperationException("Plot contains duplicate element IDs.");
            Prepare(snapshot, element);
            return new MapGenerationResult(snapshot);
        }

        /// <summary>
        /// Prepares an existing location for placement on the generated map.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="element">The element to process.</param>
        /// <param name="generator">The generator value.</param>
        /// <returns>The prepared location returned by the generator.</returns>
        /// <exception cref="ArgumentException">Thrown when the element is not a location or has no placement in the snapshot.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the generator does not return the prepared location instance.</exception>
        public static IElement PrepareLocation(Plot plot, MapSnapshot snapshot,
            IElement element, IGenerator generator)
        {
            if (plot == null) throw new ArgumentNullException(nameof(plot));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var prepared = Prepare(snapshot, element);
            var generated = generator.Generate(plot, prepared);
            EnsureSame(prepared, generated);
            return generated;
        }

        /// <summary>
        /// Asynchronously prepares an existing location for placement on the generated map.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="element">The element to process.</param>
        /// <param name="generator">The generator value.</param>
        /// <returns>A task that represents the asynchronous operation and contains its result.</returns>
        /// <exception cref="ArgumentException">Thrown when the element is not a location or has no placement in the snapshot.</exception>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the generator does not return the prepared location instance.</exception>
        /// <seealso cref="PrepareLocation"/>
        public static async Task<IElement> PrepareLocationAsync(Plot plot, MapSnapshot snapshot,
            IElement element, IGenerator generator)
        {
            if (plot == null) throw new ArgumentNullException(nameof(plot));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var prepared = Prepare(snapshot, element);
            var generated = await generator.GenerateAsync(plot, prepared).ConfigureAwait(false);
            EnsureSame(prepared, generated);
            return generated;
        }

        /// <summary>
        /// Prepares the supplied value.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="element">The element to process.</param>
        /// <returns>The prepared location element.</returns>
        /// <exception cref="ArgumentException">Thrown when an argument is invalid.</exception>
        private static IElement Prepare(MapSnapshot snapshot, IElement element)
        {
            if (element.Type != ElemType.Location)
                throw new ArgumentException("Only Location elements can be prepared.", nameof(element));
            var placement = snapshot.Placements.FirstOrDefault(candidate => candidate.ElementId == element.Id);
            if (placement == null)
                throw new ArgumentException("The snapshot does not contain a placement for this element.", nameof(element));
            var keys = MapParams.For(snapshot.MapId);
            element.Params.SetOrAdd(keys.Position, placement.Position);
            element.Params.SetOrAdd(keys.Biome, placement.Biome);
            element.Params.SetOrAdd(keys.MapId, snapshot.MapId);
            element.Params.SetOrAdd(keys.Seed, snapshot.Seed);
            element.Params.SetOrAdd(keys.AlgorithmVersion, snapshot.AlgorithmVersion);
            return element;
        }

        /// <summary>
        /// Ensures that generation returned the prepared element instance.
        /// </summary>
        /// <param name="prepared">The prepared value.</param>
        /// <param name="generated">The generated value.</param>
        /// <exception cref="InvalidOperationException">Thrown when the operation cannot be completed in the current state.</exception>
        private static void EnsureSame(IElement prepared, IElement? generated)
        {
            if (generated == null || !ReferenceEquals(prepared, generated))
                throw new InvalidOperationException("Generator must return the prepared location instance.");
        }
    }
}
