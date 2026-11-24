using System;
using System.Collections.Generic;
using BaseClasses.Enum;
using BaseClasses.Model;
using Map.Core.Model;
using Map.Core.Params;
using Map.Core.Generation;
using Map.Core.Roads;
using Map.Core.Validation;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class MapGenerationResultTests
    {
        [Theory]
        [InlineData(MapApplicationMode.RejectConflicts)]
        [InlineData(MapApplicationMode.ReplaceMap)]
        public void ApplyTo_ProjectsOnlyStoryRoadsAndWritesOnlyStoryPlacements(MapApplicationMode mode)
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var item = new Element(ElemType.Item, "Unrelated item");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            plot.Add(item);
            var snapshot = SnapshotWithLocationRoad(first.Id, second.Id);
            var storyRoad = snapshot.Roads[0];
            var keys = MapParams.For(snapshot.MapId);

            new MapGenerationResult(snapshot).ApplyTo(plot, mode);

            Assert.Equal(new GridPosition(0, 0), first.Params[keys.Position]);
            Assert.Equal(new GridPosition(1, 0), second.Params[keys.Position]);
            Assert.Equal(new BiomeId("coast"), first.Params[keys.Biome]);
            Assert.Equal(snapshot.MapId, first.Params[keys.MapId]);
            Assert.Equal(snapshot.Seed, first.Params[keys.Seed]);
            Assert.Equal(snapshot.AlgorithmVersion, first.Params[keys.AlgorithmVersion]);
            Assert.Empty(item.Params.Keys);
            Assert.Equal(3, plot.Elements.Count);
            Assert.Equal(storyRoad.RoadId, Assert.Single(plot.Relations).Value);
            Assert.Single(snapshot.Roads);
            Assert.Equal(new GridPosition(0, 0), snapshot.Roads[0].GridPath[0]);
            RoadProjectionValidator.Validate(plot, snapshot.Roads, snapshot.MapId);
        }

        [Fact]
        public void ApplyTo_WithLocationRoadRejectsStoryRoadConflictBeforeWriting()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            var snapshot = SnapshotWithLocationRoad(first.Id, second.Id);
            var existing = RoadRelationProjection.Create(plot, snapshot.Roads[0], snapshot.MapId);

            Assert.Throws<InvalidOperationException>(() => new MapGenerationResult(snapshot).ApplyTo(plot));

            Assert.Empty(first.Params.Keys);
            Assert.Empty(second.Params.Keys);
            Assert.Same(existing, Assert.Single(plot.Relations));
        }

        [Fact]
        public void ApplyTo_ReplaceMapWithInvalidSnapshotPreservesExistingState()
        {
            var location = new Element(ElemType.Location, "Harbor");
            var plot = new Plot();
            plot.Add(location);
            var valid = Snapshot(location.Id, new GridPosition(0, 0));
            var keys = MapParams.For(valid.MapId);
            location.Params.Add(keys.Position, new GridPosition(3, 3));
            var existing = RoadRelationProjection.Create(plot,
                new Road(Guid.NewGuid(), location.Id, location.Id), valid.MapId);
            var invalid = new MapSnapshot(valid.MapId, valid.Dimensions, valid.Seed, valid.AlgorithmVersion,
                valid.Biomes, Array.Empty<MapCell>(), valid.Placements, valid.Roads);

            Assert.Throws<MapValidationException>(() => new MapGenerationResult(invalid)
                .ApplyTo(plot, MapApplicationMode.ReplaceMap));

            Assert.Equal(new GridPosition(3, 3), location.Params[keys.Position]);
            Assert.Same(existing, Assert.Single(plot.Relations));
        }

        [Fact]
        public void ApplyTo_WritesPositionOnlyWhenExplicitlyCalled()
        {
            var plot = new Plot();
            var location = new Element(ElemType.Location, "Harbor");
            plot.Add(location);
            var snapshot = Snapshot(location.Id, new GridPosition(2, 3));
            var keys = MapParams.For(snapshot.MapId);

            var result = new MapGenerationResult(snapshot);

            Assert.False(location.Params.ContainsKey(keys.Position));
            result.ApplyTo(plot);

            Assert.Equal(new GridPosition(2, 3), location.Params[keys.Position]);
        }

        [Fact]
        public void ApplyTo_RejectsConflictsBeforeWritingAnyElement()
        {
            var plot = new Plot();
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            plot.Add(first);
            plot.Add(second);
            var snapshot = Snapshot(first.Id, new GridPosition(0, 0), second.Id, new GridPosition(1, 0));
            var keys = MapParams.For(snapshot.MapId);
            second.Params.Add(keys.Biome, new BiomeId("old"));

            Assert.Throws<InvalidOperationException>(() => new MapGenerationResult(snapshot).ApplyTo(plot));
            Assert.False(first.Params.ContainsKey(keys.Position));
        }

        [Fact]
        public void ApplyTo_ReplaceMapOnlyReplacesItsFiveKeys()
        {
            var plot = new Plot();
            var location = new Element(ElemType.Location, "Harbor");
            plot.Add(location);
            var snapshot = Snapshot(location.Id, new GridPosition(1, 1));
            var keys = MapParams.For(snapshot.MapId);
            var unrelated = new BaseClasses.Model.Params.ParamKey<string>("Note", "Other");
            location.Params.Add(keys.Position, new GridPosition(9, 9));
            location.Params.Add(unrelated, "keep");

            new MapGenerationResult(snapshot).ApplyTo(plot, MapApplicationMode.ReplaceMap);

            Assert.Equal(new GridPosition(1, 1), location.Params[keys.Position]);
            Assert.Equal("keep", location.Params[unrelated]);
        }

        [Fact]
        public void ApplyTo_ReplaceMapRemovesStaleParametersAndRoadsForMapNamespace()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var third = new Element(ElemType.Location, "Third");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            plot.Add(third);
            var snapshot = Snapshot(first.Id, new GridPosition(0, 0), second.Id, new GridPosition(1, 0));
            var keys = MapParams.For(snapshot.MapId);
            var staleRoadId = Guid.NewGuid();
            var staleRoadKey = new BaseClasses.Model.Params.ParamKey<Guid>($"Road/{staleRoadId:D}", keys.Position.Namespace);
            var staleKey = new BaseClasses.Model.Params.ParamKey<string>("Stale", keys.Position.Namespace);
            first.Params.Add(staleKey, "old");
            third.Params.Add(keys.Biome, new BiomeId("coast"));
            plot.Relations.Add(new BaseClasses.Model.Relation(first, third, staleRoadKey, staleRoadId));

            new MapGenerationResult(snapshot).ApplyTo(plot, MapApplicationMode.ReplaceMap);

            Assert.False(first.Params.ContainsKey(staleKey));
            Assert.False(third.Params.ContainsKey(keys.Biome));
            Assert.Empty(plot.Relations);
        }

        [Fact]
        public void ApplyTo_RejectsDuplicateCellsBeforeWriting()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            var snapshot = Snapshot(first.Id, new GridPosition(1, 1), second.Id, new GridPosition(1, 1));

            Assert.Throws<MapValidationException>(() => new MapGenerationResult(snapshot).ApplyTo(plot));
            Assert.Empty(first.Params.Keys);
            Assert.Empty(second.Params.Keys);
        }

        [Fact]
        public void ApplyTo_RejectsMissingMembership()
        {
            var location = new Element(ElemType.Location, "Missing");

            Assert.Throws<KeyNotFoundException>(() => new MapGenerationResult(Snapshot(location.Id, new GridPosition(0, 0))).ApplyTo(new Plot()));
        }

        [Fact]
        public void ApplyTo_RejectsNonLocationTarget()
        {
            var item = new Element(ElemType.Item, "Sword");
            var plot = new Plot();
            plot.Add(item);

            Assert.Throws<ArgumentException>(() => new MapGenerationResult(Snapshot(item.Id, new GridPosition(0, 0))).ApplyTo(plot));
            Assert.Empty(item.Params.Keys);
        }

        [Fact]
        public void ApplyTo_RejectsUndefinedModeBeforeWriting()
        {
            var location = new Element(ElemType.Location, "Harbor");
            var plot = new Plot();
            plot.Add(location);

            Assert.Throws<ArgumentOutOfRangeException>(() => new MapGenerationResult(Snapshot(location.Id, new GridPosition(0, 0)))
                .ApplyTo(plot, (MapApplicationMode)99));
            Assert.Empty(location.Params.Keys);
        }

        [Fact]
        public void ApplyTo_UsesCanonicalPlotElementWhenDuplicateIdIsRejected()
        {
            var first = new FixedIdElement(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), ElemType.Location);
            var second = new FixedIdElement(first.Id, ElemType.Location);
            var plot = new Plot();
            plot.Add(first);
            Assert.False(plot.Elements.Add(second));
            var snapshot = Snapshot(first.Id, new GridPosition(0, 0));

            new MapGenerationResult(snapshot).ApplyTo(plot);

            Assert.Equal(new GridPosition(0, 0), first.Params[MapParams.For(snapshot.MapId).Position]);
            Assert.Empty(second.Params.Keys);
        }

        private sealed class FixedIdElement : BaseClasses.Interface.IElement
        {
            public Guid Id { get; }
            public ElemType Type { get; }
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public BaseClasses.Model.Params.ParamBag Params { get; } = new BaseClasses.Model.Params.ParamBag();
            public int Time { get; set; } = -1;
            public FixedIdElement(Guid id, ElemType type) { Id = id; Type = type; }
            public bool IsEmpty() => false;
        }

        private static MapSnapshot Snapshot(Guid firstId, GridPosition firstPosition,
            Guid? secondId = null, GridPosition secondPosition = default)
        {
            var placements = new List<LocationPlacement>
            {
                new LocationPlacement(firstId, firstPosition, new BiomeId("coast"))
            };
            if (secondId.HasValue)
                placements.Add(new LocationPlacement(secondId.Value, secondPosition, new BiomeId("coast")));
            return new MapSnapshot(Guid.Parse("11111111-1111-1111-1111-111111111111"),
                new MapDimensions(4, 4), 42, "v1", new[] { new BiomeId("coast") }, placements,
                Array.Empty<Road>());
        }

        private static MapSnapshot SnapshotWithLocationRoad(Guid firstId, Guid secondId)
        {
            var snapshot = Snapshot(firstId, new GridPosition(0, 0), secondId, new GridPosition(1, 0));
            return new MapSnapshot(snapshot.MapId, snapshot.Dimensions, snapshot.Seed, snapshot.AlgorithmVersion,
                snapshot.Biomes, snapshot.Cells, snapshot.Placements,
                new[]
                {
                    new Road(Guid.NewGuid(), firstId, secondId,
                        new[] { new GridPosition(0, 0), new GridPosition(1, 0) })
                });
        }
    }
}
