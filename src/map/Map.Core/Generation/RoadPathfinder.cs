using System;
using System.Collections.Generic;
using System.Linq;
using Map.Core.Model;

namespace Map.Core.Generation
{
    /// <summary>
    /// Finds deterministic grid paths between map locations under terrain constraints.
    /// </summary>
    public sealed class RoadPathfinder
    {
        private static readonly GridPosition[] NeighbourOffsets =
        {
            new GridPosition(0, -1),
            new GridPosition(-1, 0),
            new GridPosition(1, 0),
            new GridPosition(0, 1)
        };

        /// <summary>
        /// Finds path.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <param name="start">The start value.</param>
        /// <param name="end">The end value.</param>
        /// <param name="policy">The policy value.</param>
        /// <returns>The resulting collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside the supported range.</exception>
        public IReadOnlyList<GridPosition> FindPath(MapSnapshot snapshot, GridPosition start, GridPosition end,
            RoadGenerationPolicy policy)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (!snapshot.Dimensions.Contains(start))
                throw new ArgumentOutOfRangeException(nameof(start), "Start position is outside map dimensions.");
            if (!snapshot.Dimensions.Contains(end))
                throw new ArgumentOutOfRangeException(nameof(end), "End position is outside map dimensions.");

            var terrainByPosition = snapshot.Cells.ToDictionary(cell => cell.Position, cell => cell.TerrainHeight);
            if (!terrainByPosition.ContainsKey(start) || !terrainByPosition.ContainsKey(end))
                return Array.Empty<GridPosition>();

            var frontier = new List<OpenNode> { new OpenNode(start, 0d, ManhattanDistance(start, end)) };
            var costs = new Dictionary<GridPosition, double> { [start] = 0d };
            var previous = new Dictionary<GridPosition, GridPosition>();

            while (frontier.Count > 0)
            {
                var currentIndex = SelectLowestPriority(frontier);
                var current = frontier[currentIndex];
                frontier.RemoveAt(currentIndex);

                if (current.Position == end)
                    return ReconstructPath(previous, current.Position);

                if (!costs.TryGetValue(current.Position, out var bestKnownCost) || current.Cost != bestKnownCost)
                    continue;

                foreach (var offset in NeighbourOffsets)
                {
                    var neighbour = new GridPosition(current.Position.X + offset.X, current.Position.Y + offset.Y);
                    if (!snapshot.Dimensions.Contains(neighbour) ||
                        !terrainByPosition.TryGetValue(neighbour, out var terrainHeight))
                        continue;

                    var nextCost = current.Cost + 1d + TerrainTraversalCost(terrainHeight, policy);
                    if (costs.TryGetValue(neighbour, out var previousCost) && nextCost >= previousCost)
                        continue;

                    costs[neighbour] = nextCost;
                    previous[neighbour] = current.Position;
                    frontier.Add(new OpenNode(neighbour, nextCost, ManhattanDistance(neighbour, end)));
                }
            }

            return Array.Empty<GridPosition>();
        }

        /// <summary>
        /// Calculates traversal cost while keeping every terrain cell traversable.
        /// Terrain above the configured preferred maximum is strongly discouraged, but never
        /// blocks a road completely.
        /// </summary>
        /// <param name="terrainHeight">The normalized terrain height.</param>
        /// <param name="policy">The road-generation policy that defines traversal costs.</param>
        /// <returns>The traversal cost for the terrain cell.</returns>
        private static double TerrainTraversalCost(float terrainHeight, RoadGenerationPolicy policy)
        {
            var baseCost = terrainHeight * policy.TerrainHeightCost;
            var excessHeight = Math.Max(0f, terrainHeight - policy.PreferredMaximumTerrainHeight);
            var difficultTerrainPenalty = excessHeight * (policy.TerrainHeightCost + 1f) * 100d;
            return baseCost + difficultTerrainPenalty;
        }

        /// <summary>
        /// Selects the frontier node with the lowest pathfinding priority.
        /// </summary>
        /// <param name="frontier">The frontier value.</param>
        /// <returns>The computed value.</returns>
        private static int SelectLowestPriority(IReadOnlyList<OpenNode> frontier)
        {
            var bestIndex = 0;
            for (var index = 1; index < frontier.Count; index++)
                if (frontier[index].CompareTo(frontier[bestIndex]) < 0)
                    bestIndex = index;
            return bestIndex;
        }

        /// <summary>
        /// Reconstructs the path by following the predecessor map.
        /// </summary>
        /// <param name="previous">The previous value.</param>
        /// <param name="end">The end value.</param>
        /// <returns>The resulting collection.</returns>
        private static IReadOnlyList<GridPosition> ReconstructPath(
            IReadOnlyDictionary<GridPosition, GridPosition> previous, GridPosition end)
        {
            var path = new List<GridPosition> { end };
            var current = end;
            while (previous.TryGetValue(current, out var predecessor))
            {
                path.Add(predecessor);
                current = predecessor;
            }

            path.Reverse();
            return path.AsReadOnly();
        }

        /// <summary>
        /// Calculates the Manhattan distance between two grid positions.
        /// </summary>
        /// <param name="first">The first value.</param>
        /// <param name="second">The second value.</param>
        /// <returns>The computed value.</returns>
        private static int ManhattanDistance(GridPosition first, GridPosition second) =>
            Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

        /// <summary>
        /// Represents one prioritized node in the road pathfinder's open set.
        /// </summary>
        private readonly struct OpenNode : IComparable<OpenNode>
        {
            public GridPosition Position { get; }
            public double Cost { get; }
            private double Priority { get; }
            private int Heuristic { get; }

            /// <summary>
            /// Initializes a new instance of <see cref="OpenNode"/>.
            /// </summary>
            /// <param name="position">The grid position to process.</param>
            /// <param name="cost">The cost value.</param>
            /// <param name="heuristic">The heuristic value.</param>
            public OpenNode(GridPosition position, double cost, int heuristic)
            {
                Position = position;
                Cost = cost;
                Heuristic = heuristic;
                Priority = cost + heuristic;
            }

            /// <summary>
            /// Compares open nodes by pathfinding priority.
            /// </summary>
            /// <param name="other">The other value.</param>
            /// <returns>The computed value.</returns>
            public int CompareTo(OpenNode other)
            {
                var comparison = Priority.CompareTo(other.Priority);
                if (comparison != 0) return comparison;
                comparison = Heuristic.CompareTo(other.Heuristic);
                if (comparison != 0) return comparison;
                comparison = Position.Y.CompareTo(other.Position.Y);
                return comparison != 0 ? comparison : Position.X.CompareTo(other.Position.X);
            }
        }
    }
}
