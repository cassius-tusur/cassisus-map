using System;
using System.Collections.Generic;
using System.Linq;
using BaseClasses.Enum;
using BaseClasses.Interface;
using BaseClasses.Model;
using BaseClasses.Services.Binds;
using Map.Core.Model;

namespace Map.Core.Generation
{
    /// <summary>
    /// Generates deterministic map snapshots from story locations and relations.
    /// </summary>
    /// <seealso cref="MapGenerationOptions"/>
    public sealed class MapGenerator
    {
        /// <summary>
        /// Generates a deterministic map snapshot from the plot and generation options.
        /// </summary>
        /// <param name="plot">The plot that contains the affected elements.</param>
        /// <param name="options">The generation options.</param>
        /// <returns>A generation result containing the deterministic map snapshot.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the operation cannot be completed in the current state.</exception>
        public MapGenerationResult Generate(Plot plot, MapGenerationOptions options)
        {
            if (plot == null) throw new ArgumentNullException(nameof(plot));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var locations = plot.Elements.Where(e => e.Type == ElemType.Location).OrderBy(e => e.Id).ToList();
            var ids = new HashSet<Guid>();
            foreach (var location in locations)
            {
                if (location.Id == Guid.Empty)
                    throw new InvalidOperationException("Location IDs cannot be empty.");
                if (!ids.Add(location.Id))
                    throw new InvalidOperationException("Location IDs must be unique.");
            }

            var cellCount = checked(options.Dimensions.Width * options.Dimensions.Height);
            if (locations.Count > cellCount)
                throw new InvalidOperationException("There are more locations than available map cells.");

            var terrainHeights = FractalPerlinNoise.Generate(options.Dimensions, options.Seed, options.TerrainNoise);
            var biomeNoise = FractalPerlinNoise.Generate(options.Dimensions, BiomeNoiseSeed(options.Seed), options.BiomeNoise);
            var cells = new List<MapCell>(cellCount);
            for (var y = 0; y < options.Dimensions.Height; y++)
                for (var x = 0; x < options.Dimensions.Width; x++)
                {
                    var index = y * options.Dimensions.Width + x;
                    var terrainHeight = terrainHeights[index];
                    cells.Add(new MapCell(new GridPosition(x, y), terrainHeight,
                        BiomeClassifier.Classify(terrainHeight, biomeNoise[index], options.BiomeRules)));
                }

            var freeCells = cells.ToList();

            var placementRandom = new DeterministicRandom(options.Seed);
            placementRandom.Shuffle(freeCells);

            var minimumLocationSpacing = CalculateMinimumLocationSpacing(options.Dimensions,
                locations.Count);
            var preferredPassageDistance = minimumLocationSpacing * 1.5d;
            var placements = PlaceLocations(plot, locations, freeCells, minimumLocationSpacing,
                preferredPassageDistance, placementRandom);

            var snapshotWithoutRoads = new MapSnapshot(options.MapId, options.Dimensions, options.Seed,
                options.AlgorithmVersion, options.Biomes, cells, placements, Array.Empty<Road>());
            var roads = CreateRoads(plot, snapshotWithoutRoads, options.RoadPolicy);

            return new MapGenerationResult(new MapSnapshot(options.MapId, options.Dimensions, options.Seed,
                options.AlgorithmVersion, options.Biomes, cells, placements, roads));
        }

        /// <summary>
        /// Creates roads.
        /// </summary>
        /// <param name="plot">The plot that supplies passage relations between placed locations.</param>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="policy">The policy value.</param>
        /// <returns>The resulting collection.</returns>
        private static List<Road> CreateRoads(Plot plot, MapSnapshot snapshot, RoadGenerationPolicy policy)
        {
            if (!policy.ConnectFeatures)
                return new List<Road>();

            var features = snapshot.Placements.Select(placement => new MapFeature(placement.ElementId, placement.Position))
                .OrderBy(feature => feature.Id)
                .ToList();
            var featuresById = features.ToDictionary(feature => feature.Id);
            var roadPairs = new HashSet<FeaturePair>();
            foreach (var relation in plot.Relations.Where(IsActivePassage))
            {
                if (relation.Source == null || relation.Target == null ||
                    relation.Source.Id == relation.Target.Id ||
                    !featuresById.ContainsKey(relation.Source.Id) ||
                    !featuresById.ContainsKey(relation.Target.Id))
                    continue;

                roadPairs.Add(new FeaturePair(relation.Source.Id, relation.Target.Id));
            }

            var pathfinder = new RoadPathfinder();
            var roads = new List<Road>();
            foreach (var pair in roadPairs.OrderBy(pair => pair.FirstId).ThenBy(pair => pair.SecondId))
            {
                var path = pathfinder.FindPath(snapshot, featuresById[pair.FirstId].Position,
                    featuresById[pair.SecondId].Position, policy);
                if (path.Count == 0)
                    continue;

                roads.Add(new Road(
                    DeterministicRandom.CreateStableGuid(snapshot.MapId, "road", $"{pair.FirstId:D}|{pair.SecondId:D}"),
                    pair.FirstId, pair.SecondId, path));
            }

            return roads;
        }

        /// <summary>
        /// Checks whether a relation is an active base passage.
        /// Map.Core intentionally avoids a project dependency on LocationGenerator;
        /// it recognizes the stable base relation contract.
        /// </summary>
        /// <param name="relation">The relation to inspect.</param>
        /// <returns><see langword="true"/> if the relation is an active location passage; otherwise, <see langword="false"/>.</returns>
        private static bool IsActivePassage(Relation relation)
        {
            return relation?.Value is bool active && active &&
                   relation.Param.Equals(BaseRelationKeys.Passage);
        }

        /// <summary>
        /// Places locations with a preference for cells close to already placed
        /// passage-neighbours. Disconnected components retain deterministic
        /// shuffled placement.
        /// </summary>
        /// <param name="plot">The plot that supplies passage relations between locations.</param>
        /// <param name="locations">The story locations to place.</param>
        /// <param name="freeCells">The available map cells.</param>
        /// <param name="minimumLocationSpacing">The preferred minimum spacing between placements.</param>
        /// <param name="preferredPassageDistance">The preferred distance between passage neighbours.</param>
        /// <param name="random">The deterministic random source used to break placement ties.</param>
        /// <returns>The generated location placements.</returns>
        private static List<LocationPlacement> PlaceLocations(Plot plot,
            IReadOnlyList<IElement> locations, List<MapCell> freeCells,
            double minimumLocationSpacing, double preferredPassageDistance,
            DeterministicRandom random)
        {
            var adjacency = locations.ToDictionary(location => location.Id,
                _ => new List<Guid>());
            var locationIds = adjacency.Keys.ToHashSet();
            foreach (var relation in plot.Relations.Where(IsActivePassage))
            {
                if (!locationIds.Contains(relation.Source.Id) ||
                    !locationIds.Contains(relation.Target.Id) ||
                    relation.Source.Id == relation.Target.Id)
                    continue;

                if (!adjacency[relation.Source.Id].Contains(relation.Target.Id))
                    adjacency[relation.Source.Id].Add(relation.Target.Id);
                if (!adjacency[relation.Target.Id].Contains(relation.Source.Id))
                    adjacency[relation.Target.Id].Add(relation.Source.Id);
            }

            if (!adjacency.Values.Any(neighbours => neighbours.Count > 0))
                return PlaceLocationsInShuffledCells(locations, freeCells);

            var remaining = locations.Select(location => location.Id).ToHashSet();
            var placed = new Dictionary<Guid, GridPosition>();
            var placements = new List<LocationPlacement>(locations.Count);

            while (remaining.Count > 0)
            {
                var nextId = remaining
                    .Select(id => new
                    {
                        Id = id,
                        PlacedNeighbours = adjacency[id].Count(neighbour => placed.ContainsKey(neighbour))
                    })
                    .OrderByDescending(candidate => candidate.PlacedNeighbours)
                    .ThenBy(candidate => candidate.Id)
                    .First().Id;
                var location = locations.First(candidate => candidate.Id == nextId);
                var placedNeighbours = adjacency[nextId]
                    .Where(placed.ContainsKey)
                    .Select(neighbour => placed[neighbour])
                    .ToArray();

                var spacedCells = freeCells
                    .Where(candidate => placed.Values.All(position =>
                        SquaredDistance(candidate.Position, position) >=
                        minimumLocationSpacing * minimumLocationSpacing))
                    .ToList();
                var candidates = spacedCells.Count > 0 ? spacedCells : freeCells;
                var rankedCandidates = placedNeighbours.Length == 0
                    ? candidates
                        .OrderByDescending(candidate => placed.Values.Sum(position =>
                            SquaredDistance(candidate.Position, position)))
                    : candidates
                        .OrderBy(candidate => PassageDistanceScore(candidate.Position,
                            placedNeighbours, preferredPassageDistance))
                        .ThenByDescending(candidate => placed.Values.Min(position =>
                            SquaredDistance(candidate.Position, position)));
                var bestCandidates = rankedCandidates
                    .Take(Math.Min(16, candidates.Count))
                    .ToList();
                var cell = bestCandidates[(int)(random.NextUInt() % (uint)bestCandidates.Count)];

                freeCells.Remove(cell);
                placed[nextId] = cell.Position;
                placements.Add(new LocationPlacement(nextId, cell.Position, cell.Biome, location.Name));
                remaining.Remove(nextId);
            }

            return placements;
        }

        /// <summary>
        /// Calculates the preferred minimum spacing between generated locations.
        /// </summary>
        /// <param name="dimensions">The map dimensions.</param>
        /// <param name="locationCount">The number of locations to place.</param>
        /// <returns>The preferred spacing measured in grid cells.</returns>
        private static double CalculateMinimumLocationSpacing(MapDimensions dimensions,
            int locationCount)
        {
            if (locationCount <= 1)
                return 0d;

            var mapSpan = Math.Min(dimensions.Width, dimensions.Height);
            var densitySpacing = mapSpan / Math.Max(4d, Math.Sqrt(locationCount) * 2d);
            return Math.Max(8d, densitySpacing);
        }

        /// <summary>
        /// Scores a candidate cell by its distance from already placed passage neighbours.
        /// </summary>
        /// <param name="candidate">The candidate grid position.</param>
        /// <param name="neighbours">The positions of already placed passage neighbours.</param>
        /// <param name="preferredDistance">The preferred passage distance.</param>
        /// <returns>The squared distance error; lower values are preferred.</returns>
        private static double PassageDistanceScore(GridPosition candidate,
            IReadOnlyList<GridPosition> neighbours, double preferredDistance)
        {
            var score = 0d;
            foreach (var neighbour in neighbours)
            {
                var distance = Math.Sqrt(SquaredDistance(candidate, neighbour));
                var difference = distance - preferredDistance;
                score += difference * difference;
            }

            return score;
        }

        /// <summary>
        /// Places locations into cells that are already in deterministic shuffled order.
        /// </summary>
        /// <param name="locations">The story locations to place.</param>
        /// <param name="freeCells">The shuffled cells available for placement.</param>
        /// <returns>The generated location placements.</returns>
        private static List<LocationPlacement> PlaceLocationsInShuffledCells(
            IReadOnlyList<IElement> locations, IReadOnlyList<MapCell> freeCells)
        {
            var placements = new List<LocationPlacement>(locations.Count);
            for (var index = 0; index < locations.Count; index++)
            {
                var cell = freeCells[index];
                placements.Add(new LocationPlacement(
                    locations[index].Id, cell.Position, cell.Biome, locations[index].Name));
            }

            return placements;
        }

        /// <summary>
        /// Derives the deterministic biome-noise seed from the map seed.
        /// </summary>
        /// <param name="seed">The deterministic seed.</param>
        /// <returns>The computed value.</returns>
        private static int BiomeNoiseSeed(int seed) => unchecked((int)((uint)seed + 0xA511E9B3u));

        /// <summary>
        /// Calculates the squared Euclidean distance between two grid positions.
        /// </summary>
        /// <param name="first">The first value.</param>
        /// <param name="second">The second value.</param>
        /// <returns>The computed value.</returns>
        private static double SquaredDistance(GridPosition first, GridPosition second)
        {
            var x = (double)first.X - second.X;
            var y = (double)first.Y - second.Y;
            return x * x + y * y;
        }

        /// <summary>
        /// Associates a story element with its generated map placement.
        /// </summary>
        private readonly struct MapFeature
        {
            public Guid Id { get; }
            public GridPosition Position { get; }

            /// <summary>
            /// Initializes a new instance of <see cref="MapFeature"/>.
            /// </summary>
            /// <param name="id">The id value.</param>
            /// <param name="position">The grid position to process.</param>
            public MapFeature(Guid id, GridPosition position)
            {
                Id = id;
                Position = position;
            }
        }

        /// <summary>
        /// Represents an unordered pair of generated map features.
        /// </summary>
        private readonly struct FeaturePair : IEquatable<FeaturePair>
        {
            public Guid FirstId { get; }
            public Guid SecondId { get; }

            /// <summary>
            /// Initializes a new instance of <see cref="FeaturePair"/>.
            /// </summary>
            /// <param name="firstId">The first identifier.</param>
            /// <param name="secondId">The second identifier.</param>
            public FeaturePair(Guid firstId, Guid secondId)
            {
                if (firstId.CompareTo(secondId) <= 0)
                {
                    FirstId = firstId;
                    SecondId = secondId;
                }
                else
                {
                    FirstId = secondId;
                    SecondId = firstId;
                }
            }

            /// <summary>
            /// Determines whether the value is equal to the supplied value.
            /// </summary>
            /// <param name="other">The other value.</param>
            /// <returns><see langword="true"/> if the value is equal to the supplied value; otherwise, <see langword="false"/>.</returns>
            public bool Equals(FeaturePair other) => FirstId == other.FirstId && SecondId == other.SecondId;
            /// <summary>
            /// Determines whether the value is equal to the supplied value.
            /// </summary>
            /// <param name="obj">The obj value.</param>
            /// <returns><see langword="true"/> if the value is equal to the supplied value; otherwise, <see langword="false"/>.</returns>
            public override bool Equals(object? obj) => obj is FeaturePair other && Equals(other);
            /// <summary>
            /// Returns a hash code for the value.
            /// </summary>
            /// <returns>The computed value.</returns>
            public override int GetHashCode() => HashCode.Combine(FirstId, SecondId);
        }
    }
}
