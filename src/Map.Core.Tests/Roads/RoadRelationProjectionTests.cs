using System;
using System.Collections.Generic;
using BaseClasses.Enum;
using BaseClasses.Model;
using BaseClasses.Model.Params;
using Map.Core.Generation;
using Map.Core.Model;
using Map.Core.Roads;
using Map.Core.Validation;
using Xunit;

namespace Map.Core.Tests.Roads
{
    public class RoadRelationProjectionTests
    {
        private static readonly Guid MapId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public void Create_RejectsNonLocationEndpointWithoutAddingRelation()
        {
            var location = new Element(ElemType.Location, "Harbor");
            var item = new Element(ElemType.Item, "Sword");
            var plot = new Plot();
            plot.Add(location);
            plot.Add(item);
            var road = new Road(Guid.NewGuid(), location.Id, item.Id);

            Assert.Throws<ArgumentException>(() => RoadRelationProjection.Create(plot, road, MapId));
            Assert.Empty(plot.Relations);
        }

        [Fact]
        public void Validator_RequiresProjectionsOnlyForRoadsBetweenPlotLocations()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var item = new Element(ElemType.Item, "Sword");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            plot.Add(item);
            var storyRoad = new Road(Guid.NewGuid(), first.Id, second.Id);
            var decorationId = Guid.NewGuid();
            var roads = new[]
            {
                new Road(Guid.NewGuid(), decorationId, first.Id),
                new Road(Guid.NewGuid(), second.Id, decorationId),
                new Road(Guid.NewGuid(), decorationId, Guid.NewGuid()),
                new Road(Guid.NewGuid(), first.Id, item.Id),
                storyRoad
            };
            RoadRelationProjection.Create(plot, storyRoad, MapId);

            RoadProjectionValidator.Validate(plot, roads, MapId);

            Assert.Single(plot.Relations);
        }

        [Fact]
        public void Validator_StillRejectsDuplicateVisualRoadIds()
        {
            var road = new Road(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            Assert.Throws<MapValidationException>(() => RoadProjectionValidator.Validate(
                new Plot(), new[] { road, road }, MapId));
        }

        [Fact]
        public void Create_UsesUniqueRoadKeyAndCanonicalEndpointOrder()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            var road = new Road(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), second.Id, first.Id);

            var relation = RoadRelationProjection.Create(plot, road, MapId);

            Assert.Equal(road.RoadId, relation.Value);
            Assert.Equal($"Road/{road.RoadId:D}", relation.Param.Name);
            Assert.Equal($"Map/{MapId:D}", relation.Param.Namespace);
            Assert.True(relation.Source.Id.CompareTo(relation.Target.Id) < 0);
            Assert.Single(plot.Relations);
        }

        [Fact]
        public void Create_PreservesParallelRoadsBetweenSameEndpoints()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            var firstRoad = new Road(Guid.NewGuid(), first.Id, second.Id);
            var secondRoad = new Road(Guid.NewGuid(), first.Id, second.Id);

            RoadRelationProjection.Create(plot, firstRoad, MapId);
            RoadRelationProjection.Create(plot, secondRoad, MapId);

            Assert.Equal(2, plot.Relations.Count);
        }

        [Fact]
        public void Validator_RejectsMissingProjection()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            var road = new Road(Guid.NewGuid(), first.Id, second.Id);

            Assert.Throws<MapValidationException>(() => RoadProjectionValidator.Validate(
                plot, new[] { road }, MapId));
        }

        [Fact]
        public void Validator_RejectsProjectionWithWrongRoadValue()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            var road = new Road(Guid.NewGuid(), first.Id, second.Id);
            var key = new ParamKey<Guid>($"Road/{road.RoadId:D}", $"Map/{MapId:D}");
            plot.Relations.Add(new Relation(first, second, key, Guid.NewGuid()));

            Assert.Throws<MapValidationException>(() => RoadProjectionValidator.Validate(
                plot, new[] { road }, MapId));
        }

        [Fact]
        public void ApplyTo_ProjectsRoadsOnlyAfterAllConflictsAreValidated()
        {
            var first = new Element(ElemType.Location, "First");
            var second = new Element(ElemType.Location, "Second");
            var plot = new Plot();
            plot.Add(first);
            plot.Add(second);
            var road = new Road(Guid.NewGuid(), first.Id, second.Id,
                new[] { new GridPosition(0, 0), new GridPosition(1, 0) });
            var snapshot = new MapSnapshot(MapId, new MapDimensions(2, 1), 1, "v1",
                new[] { new BiomeId("plains") },
                new[]
                {
                    new LocationPlacement(first.Id, new GridPosition(0, 0), new BiomeId("plains")),
                    new LocationPlacement(second.Id, new GridPosition(1, 0), new BiomeId("plains"))
                }, new[] { road });

            var existingRoadKey = new ParamKey<Guid>($"Road/{road.RoadId:D}", $"Map/{MapId:D}");
            plot.Relations.Add(new Relation(first, second, existingRoadKey, road.RoadId));
            Assert.Throws<InvalidOperationException>(() => new MapGenerationResult(snapshot).ApplyTo(plot));
            Assert.Single(plot.Relations);
            Assert.False(first.Params.ContainsKey(new ParamKey<GridPosition>("Position", $"Map/{MapId:D}")));
        }
    }
}
