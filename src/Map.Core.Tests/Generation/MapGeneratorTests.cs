using System;
using System.Collections.Generic;
using System.Linq;
using BaseClasses.Enum;
using BaseClasses.Interface;
using BaseClasses.Model;
using BaseClasses.Model.Params;
using BaseClasses.Services.Binds;
using Map.Core.Generation;
using Map.Core.Model;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class MapGeneratorTests
    {
        private static readonly Guid MapId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public void Generate_SamePlotSeedAndOptions_ProducesEqualSnapshot()
        {
            var plot = new Plot();
            var first = new FixedIdElement(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var second = new FixedIdElement(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            plot.Add(second);
            plot.Add(first);
            AddActivePassage(plot, first, second);
            var options = new MapGenerationOptions(MapId, new MapDimensions(4, 3), 42,
                new[] { new BiomeId("coast"), new BiomeId("forest") }, "v1");
            var generator = new MapGenerator();

            var left = generator.Generate(plot, options).Snapshot;
            var right = generator.Generate(plot, options).Snapshot;

            Assert.Equal(left.MapId, right.MapId);
            Assert.Equal(left.Dimensions, right.Dimensions);
            Assert.Equal(left.Seed, right.Seed);
            Assert.Equal(left.AlgorithmVersion, right.AlgorithmVersion);
            Assert.Equal(left.Biomes, right.Biomes);
            Assert.Equal(left.Cells, right.Cells);
            Assert.Equal(left.Placements, right.Placements);
            AssertRoadsEqual(left.Roads, right.Roads);
            Assert.Equal(new[] { first.Id, second.Id }.OrderBy(id => id), left.Placements.Select(p => p.ElementId));
            Assert.Equal(12, left.Cells.Count);
            Assert.Equal(Enumerable.Range(0, 3)
                .SelectMany(y => Enumerable.Range(0, 4).Select(x => new GridPosition(x, y))),
                left.Cells.Select(cell => cell.Position));
            Assert.Single(left.Roads);
            Assert.NotEmpty(left.Roads[0].GridPath);
            Assert.Empty(first.Params.Keys);
            Assert.Empty(second.Params.Keys);
        }

        [Fact]
        public void Generate_BasePassage_CreatesRoad()
        {
            var plot = new Plot();
            var first = new FixedIdElement(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var second = new FixedIdElement(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            plot.Add(first);
            plot.Add(second);
            plot.Bind(first, second, BaseRelationKeys.Passage, true);

            var snapshot = new MapGenerator().Generate(plot, new MapGenerationOptions(MapId,
                new MapDimensions(4, 3), 42, new[] { new BiomeId("plains") }, "v1")).Snapshot;

            Assert.Single(snapshot.Roads);
        }

        [Fact]
        public void Generate_UsesFreeCellsAndDeterministicPassageRoads()
        {
            var plot = new Plot();
            var first = new FixedIdElement(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var second = new FixedIdElement(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            var third = new FixedIdElement(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
            plot.Add(first);
            plot.Add(second);
            plot.Add(third);
            AddActivePassage(plot, first, second);
            AddActivePassage(plot, second, third);
            var coast = new BiomeId("coast");
            var forest = new BiomeId("forest");
            var options = new MapGenerationOptions(
                MapId,
                new MapDimensions(6, 5),
                42,
                new[] { coast, forest },
                "v1",
                new NoiseFieldOptions(9f, 3, 0.5f, 2f, 0f, 0f),
                new NoiseFieldOptions(7f, 2, 0.5f, 2f, 0f, 0f),
                new[] { new BiomeRule(coast, 0.55f, 1f), new BiomeRule(forest, 1f, 1f) },
                new RoadGenerationPolicy(true, 1f, 3f));

            var snapshot = new MapGenerator().Generate(plot, options).Snapshot;
            var reversedPlot = new Plot();
            foreach (var location in plot.Elements.Reverse()) reversedPlot.Add(location);
            AddActivePassage(reversedPlot, first, second);
            AddActivePassage(reversedPlot, second, third);
            var repeated = new MapGenerator().Generate(reversedPlot, options).Snapshot;

            Assert.Equal(30, snapshot.Cells.Count);
            Assert.Equal(30, snapshot.Cells.Select(cell => cell.Position).Distinct().Count());
            Assert.Equal(snapshot.Cells, repeated.Cells);
            Assert.Equal(snapshot.Placements, repeated.Placements);
            AssertRoadsEqual(snapshot.Roads, repeated.Roads);
            Assert.All(snapshot.Placements, placement =>
            {
                Assert.Equal(CellAt(snapshot, placement.Position).Biome, placement.Biome);
            });

            Assert.NotEmpty(snapshot.Roads);
            var locationIds = snapshot.Placements.Select(placement => placement.ElementId).ToHashSet();
            Assert.All(snapshot.Roads, road =>
            {
                Assert.Contains(road.FirstFeatureId, locationIds);
                Assert.Contains(road.SecondFeatureId, locationIds);
            });
            Assert.Equal(snapshot.Roads.Count, snapshot.Roads.Select(road => road.RoadId).Distinct().Count());
            Assert.All(snapshot.Roads, road => AssertContiguousAndInBounds(snapshot, road));
        }

        [Fact]
        public void Generate_DuplicateDirectionalPassages_StoresOneUndirectedRoad()
        {
            var plot = new Plot();
            var first = new FixedIdElement(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var second = new FixedIdElement(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            plot.Add(second);
            plot.Add(first);
            AddActivePassage(plot, first, second);
            AddActivePassage(plot, second, first);
            var options = new MapGenerationOptions(MapId, new MapDimensions(5, 1), 4490,
                new[] { new BiomeId("plains") }, "v1");

            var snapshot = new MapGenerator().Generate(plot, options).Snapshot;

            var road = Assert.Single(snapshot.Roads);
            Assert.Equal(first.Id, road.FirstFeatureId);
            Assert.Equal(second.Id, road.SecondFeatureId);
            AssertContiguousAndInBounds(snapshot, road);
        }

        [Fact]
        public void Generate_WithoutPassages_DoesNotCreateProximityRoads()
        {
            var plot = new Plot();
            plot.Add(new FixedIdElement(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));
            plot.Add(new FixedIdElement(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")));
            var plains = new BiomeId("plains");
            var options = new MapGenerationOptions(
                MapId,
                new MapDimensions(1, 2),
                10,
                new[] { plains },
                "v1",
                new NoiseFieldOptions(4f, 1, 0.5f, 2f, 0f, 0f),
                new NoiseFieldOptions(4f, 1, 0.5f, 2f, 0f, 0f),
                new[] { new BiomeRule(plains, 1f, 1f) },
                new RoadGenerationPolicy(true, 0f, 1f));

            var snapshot = new MapGenerator().Generate(plot, options).Snapshot;

            Assert.Equal(2, snapshot.Placements.Count);
            Assert.Contains(snapshot.Cells, cell => cell.TerrainHeight > options.RoadPolicy.PreferredMaximumTerrainHeight);
            Assert.Empty(snapshot.Roads);
            Assert.Empty(new MapGenerator().Generate(plot, options).Snapshot.Roads);
        }

        [Fact]
        public void Generate_NamespacedLocationGeneratorPassage_IsIgnored()
        {
            var plot = new Plot();
            var first = new FixedIdElement(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var second = new FixedIdElement(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
            plot.Add(first);
            plot.Add(second);
            plot.Relations.Add(new Relation(first, second,
                new ParamKey<bool>("Passage", "LocationGenerator"), true));

            var snapshot = new MapGenerator().Generate(plot, new MapGenerationOptions(MapId,
                new MapDimensions(4, 3), 42, new[] { new BiomeId("plains") }, "v1")).Snapshot;

            Assert.Empty(snapshot.Roads);
        }

        [Fact]
        public void Generate_RejectsMoreLocationsThanCells()
        {
            var plot = new Plot();
            plot.Add(new Element(ElemType.Location, "A"));
            plot.Add(new Element(ElemType.Location, "B"));

            var options = new MapGenerationOptions(MapId, new MapDimensions(1, 1), 1,
                new[] { new BiomeId("plains") }, "v1");

            Assert.Throws<InvalidOperationException>(() => new MapGenerator().Generate(plot, options));
        }

        [Fact]
        public void Options_RejectsDuplicateBiomeDefinitions()
        {
            Assert.Throws<ArgumentException>(() => new MapGenerationOptions(MapId,
                new MapDimensions(2, 2), 1,
                new[] { new BiomeId("forest"), new BiomeId("forest") }, "v1"));
        }

        private static MapCell CellAt(MapSnapshot snapshot, GridPosition position) =>
            snapshot.Cells.Single(cell => cell.Position == position);

        private static void AssertRoadsEqual(IReadOnlyList<Road> expected, IReadOnlyList<Road> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            for (var index = 0; index < expected.Count; index++)
            {
                Assert.Equal(expected[index].RoadId, actual[index].RoadId);
                Assert.Equal(expected[index].FirstFeatureId, actual[index].FirstFeatureId);
                Assert.Equal(expected[index].SecondFeatureId, actual[index].SecondFeatureId);
                Assert.Equal(expected[index].GridPath, actual[index].GridPath);
            }
        }

        private static void AssertContiguousAndInBounds(MapSnapshot snapshot, Road road)
        {
            Assert.NotEmpty(road.GridPath);
            Assert.NotEqual(Guid.Empty, road.RoadId);
            var features = snapshot.Placements.Select(placement => (placement.ElementId, placement.Position))
                .ToDictionary(feature => feature.Item1, feature => feature.Position);
            Assert.Equal(features[road.FirstFeatureId], road.GridPath[0]);
            Assert.Equal(features[road.SecondFeatureId], road.GridPath[road.GridPath.Count - 1]);
            Assert.All(road.GridPath, position => Assert.True(snapshot.Dimensions.Contains(position)));
            for (var index = 1; index < road.GridPath.Count; index++)
            {
                var previous = road.GridPath[index - 1];
                var current = road.GridPath[index];
                Assert.Equal(1, Math.Abs(previous.X - current.X) + Math.Abs(previous.Y - current.Y));
            }
        }

        private static void AddActivePassage(Plot plot, IElement source, IElement target) =>
            plot.Bind(source, target, BaseRelationKeys.Passage, true);

        private sealed class FixedIdElement : IElement
        {
            public Guid Id { get; }
            public ElemType Type => ElemType.Location;
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public ParamBag Params { get; } = new ParamBag();
            public int Time { get; set; } = -1;

            public FixedIdElement(Guid id) => Id = id;
            public bool IsEmpty() => false;
        }
    }
}
