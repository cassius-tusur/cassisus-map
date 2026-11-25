using System.Collections.Generic;
using Map.Core.Model;

namespace Map.Core.Benchmarks
{
    internal interface IRoadGeometryStorage
    {
        IReadOnlyList<GridPosition> Read(MapSnapshot snapshot, Road road);
    }

    internal sealed class StoredGridPathStorage : IRoadGeometryStorage
    {
        public IReadOnlyList<GridPosition> Read(MapSnapshot snapshot, Road road) => road.GridPath;
    }
}
