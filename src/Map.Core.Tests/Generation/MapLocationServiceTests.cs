using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseClasses.Enum;
using BaseClasses.Interface;
using BaseClasses.Model;
using Map.Core.Model;
using Map.Core.Params;
using Map.Core.Generation;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class MapLocationServiceTests
    {
        [Fact]
        public void PlaceExistingLocation_RejectsNonLocation()
        {
            var element = new Element(ElemType.Item, "Sword");
            var snapshot = Snapshot(element.Id);

            Assert.Throws<ArgumentException>(() => MapLocationService.PlaceExistingLocation(new Plot(), snapshot, element));
        }

        [Fact]
        public void PlaceExistingLocation_RejectsLocationOutsidePlot()
        {
            var location = new Element(ElemType.Location, "Absent");

            Assert.Throws<KeyNotFoundException>(() => MapLocationService.PlaceExistingLocation(new Plot(), Snapshot(location.Id), location));
        }

        [Fact]
        public void PlaceExistingLocation_AssignsSnapshotContext()
        {
            var location = new Element(ElemType.Location, "Harbor");
            var plot = new Plot();
            plot.Add(location);
            var snapshot = Snapshot(location.Id);

            MapLocationService.PlaceExistingLocation(plot, snapshot, location);

            var keys = MapParams.For(snapshot.MapId);
            Assert.Equal(snapshot.Placements[0].Position, location.Params[keys.Position]);
            Assert.Equal(snapshot.Placements[0].Biome, location.Params[keys.Biome]);
            Assert.Equal(snapshot.MapId, location.Params[keys.MapId]);
            Assert.Equal(snapshot.Seed, location.Params[keys.Seed]);
            Assert.Equal(snapshot.AlgorithmVersion, location.Params[keys.AlgorithmVersion]);
        }

        [Fact]
        public void PrepareLocation_DoesNotAddGeneratedLocationToPlot()
        {
            var plot = new Plot();
            var location = new Element(ElemType.Location, "Harbor");
            var snapshot = Snapshot(location.Id);
            var generated = MapLocationService.PrepareLocation(plot, snapshot, location, new EchoGenerator());

            Assert.DoesNotContain(generated, plot.Elements);
        }

        [Fact]
        public async Task PrepareLocationAsync_PopulatesContextBeforeGenerator()
        {
            var plot = new Plot();
            var location = new Element(ElemType.Location, "Harbor");
            var snapshot = Snapshot(location.Id);
            var generator = new ContextCheckingGenerator(snapshot);

            var generated = await MapLocationService.PrepareLocationAsync(plot, snapshot, location, generator);

            Assert.Same(generator.Prepared, generated);
        }

        [Fact]
        public void PrepareLocation_PopulatesContextBeforeGenerator()
        {
            var snapshot = Snapshot(Guid.NewGuid());
            var generator = new ContextCheckingGenerator(snapshot);
            var location = new Element(ElemType.Location, "Harbor");

            snapshot = Snapshot(location.Id);
            var generated = MapLocationService.PrepareLocation(new Plot(), snapshot, location, generator);

            Assert.Same(generator.Prepared, generated);
        }

        [Fact]
        public void PrepareLocation_RejectsNullGeneratorResult()
        {
            var location = new Element(ElemType.Location, "Harbor");
            var snapshot = Snapshot(location.Id);

            Assert.Throws<InvalidOperationException>(() => MapLocationService.PrepareLocation(new Plot(), snapshot,
                location, new ReturningGenerator(null)));
        }

        [Fact]
        public async Task PrepareLocationAsync_RejectsDifferentGeneratorResult()
        {
            var location = new Element(ElemType.Location, "Harbor");
            var snapshot = Snapshot(location.Id);

            await Assert.ThrowsAsync<InvalidOperationException>(() => MapLocationService.PrepareLocationAsync(new Plot(), snapshot,
                location, new ReturningGenerator(new Element(ElemType.Location))));
        }

        [Fact]
        public void PrepareLocation_UsesElementIdFromSnapshotAndCanBeApplied()
        {
            var location = new Element(ElemType.Location, "Harbor");
            var snapshot = Snapshot(location.Id);
            var prepared = MapLocationService.PrepareLocation(new Plot(), snapshot, location, new EchoGenerator());
            var plot = new Plot();
            plot.Add(prepared);

            new MapGenerationResult(snapshot).ApplyTo(plot, MapApplicationMode.ReplaceMap);

            Assert.Equal(location.Id, prepared.Id);
            Assert.Equal(snapshot.Placements[0].Position, prepared.Params[MapParams.For(snapshot.MapId).Position]);
        }

        private static MapSnapshot Snapshot(Guid id) => new MapSnapshot(
            Guid.Parse("11111111-1111-1111-1111-111111111111"), new MapDimensions(2, 2), 7, "v1",
            new[] { new BiomeId("coast") },
            new[] { new LocationPlacement(id, new GridPosition(1, 0), new BiomeId("coast")) },
            Array.Empty<Road>());

        private sealed class EchoGenerator : IGenerator
        {
            public IElement Generate(Plot plot, IElement preparedElement) => preparedElement;
            public Task<IElement> GenerateAsync(Plot plot, IElement preparedElement) => Task.FromResult(preparedElement);
        }

        private sealed class ReturningGenerator : IGenerator
        {
            private readonly IElement? _result;
            public ReturningGenerator(IElement? result) => _result = result;
            public IElement Generate(Plot plot, IElement preparedElement) => _result!;
            public Task<IElement> GenerateAsync(Plot plot, IElement preparedElement) => Task.FromResult(_result!);
        }

        private sealed class ContextCheckingGenerator : IGenerator
        {
            private readonly MapSnapshot _snapshot;
            public IElement Prepared { get; private set; } = null!;
            public ContextCheckingGenerator(MapSnapshot snapshot) => _snapshot = snapshot;
            public IElement Generate(Plot plot, IElement preparedElement) => Check(preparedElement);
            public Task<IElement> GenerateAsync(Plot plot, IElement preparedElement) =>
                Task.FromResult(Check(preparedElement));
            private IElement Check(IElement element)
            {
                var keys = MapParams.For(_snapshot.MapId);
                Assert.Equal(_snapshot.Placements[0].Position, element.Params[keys.Position]);
                Assert.Equal(_snapshot.Placements[0].Biome, element.Params[keys.Biome]);
                Assert.Equal(_snapshot.MapId, element.Params[keys.MapId]);
                Assert.Equal(_snapshot.Seed, element.Params[keys.Seed]);
                Assert.Equal(_snapshot.AlgorithmVersion, element.Params[keys.AlgorithmVersion]);
                Prepared = element;
                return element;
            }
        }
    }
}
