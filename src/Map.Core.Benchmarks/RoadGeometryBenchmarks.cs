using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Map.Core.Model;
using Map.Core.Serialization;

namespace Map.Core.Benchmarks
{
    [MemoryDiagnoser]
    public class RoadGeometryBenchmarks
    {
        private readonly MapSnapshotSerializer _serializer = new MapSnapshotSerializer();
        private readonly IRoadGeometryStorage _stored = new StoredGridPathStorage();
        private MapSnapshot _snapshot = null!;
        private Road _road = null!;

        [Params(64, 256)]
        public int Size { get; set; }

        [Params(true, false)]
        public bool Diagonal { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _snapshot = Fixture(Size, Diagonal);
            _road = _snapshot.Roads[0];
        }

        [Benchmark(Baseline = true)]
        public IReadOnlyList<GridPosition> StoredPathRestoration() => _stored.Read(_snapshot, _road);

        [Benchmark]
        public int StoredSnapshotSerializedBytes() => _serializer.Serialize(_snapshot).Length;

        private static MapSnapshot Fixture(int size, bool diagonal)
        {
            Guid first = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            Guid second = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var path = new List<GridPosition>(size);
            for (int i = 0; i < size; i++) path.Add(new GridPosition(i, diagonal ? i : 0));
            var roads = new[] { new Road(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), first, second, path) };
            return new MapSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                new MapDimensions(size, size), 42, "v1", new[] { new BiomeId("plains") },
                    new[] { new LocationPlacement(first, new GridPosition(0, 0), new BiomeId("plains")),
                    new LocationPlacement(second, new GridPosition(size - 1, diagonal ? size - 1 : 0), new BiomeId("plains")) }, roads);
        }

        public static void Main(string[] args) => BenchmarkRunner.Run<RoadGeometryBenchmarks>();
    }
}
