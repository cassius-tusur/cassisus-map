using System;
using System.Collections.Generic;
using Map.Core.Generation;
using Map.Core.Model;
using Map.Core.Validation;
using Xunit;

namespace Map.Core.Tests.Validation
{
    public class MapSnapshotValidatorTests
    {
        private static readonly Guid MapId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid FirstId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid SecondId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        [Theory]
        [InlineData("missing")]
        [InlineData("extra")]
        [InlineData("duplicate")]
        [InlineData("reordered")]
        [InlineData("null")]
        public void Validate_RejectsIncompleteOrNonRowMajorGrid(string defect)
        {
            var cells = new List<MapCell>(CompleteCells());
            switch (defect)
            {
                case "missing": cells.RemoveAt(3); break;
                case "extra": cells.Add(Cell(0, 0)); break;
                case "duplicate": cells[3] = Cell(0, 0); break;
                case "reordered": cells.Reverse(); break;
                case "null": cells[3] = null!; break;
            }

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(CompleteSnapshot(cells: cells)));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(2, 0)]
        [InlineData(0, -1)]
        [InlineData(0, 2)]
        public void Validate_RejectsCellOutsideDimensions(int x, int y)
        {
            var cells = CompleteCells();
            cells[0] = Cell(x, y);

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(CompleteSnapshot(cells: cells)));
        }

        [Fact]
        public void Validate_RejectsUnknownCellBiome()
        {
            var cells = CompleteCells();
            cells[0] = new MapCell(new GridPosition(0, 0), 0.5f, new BiomeId("unknown"));

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(CompleteSnapshot(cells: cells)));
        }

        [Fact]
        public void Validate_RejectsDefaultDimensions()
        {
            var snapshot = new MapSnapshot(MapId, default, 42, "v1", new[] { new BiomeId("plains") },
                Array.Empty<MapCell>(), Array.Empty<LocationPlacement>(), Array.Empty<Road>());

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(snapshot));
        }

        [Fact]
        public void Validate_RejectsUnknownPlacementBiome()
        {
            var placement = new LocationPlacement(FirstId, new GridPosition(0, 0), new BiomeId("unknown"));

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(
                CompleteSnapshot(placements: new[] { placement })));
        }

        [Fact]
        public void Validate_RejectsRoadWithoutStoredPath()
        {
            var snapshot = Snapshot(new[] { Placement(FirstId, 0, 0), Placement(SecondId, 1, 0) },
                new[] { new Road(Guid.NewGuid(), FirstId, SecondId) });

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(snapshot));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(2, 0)]
        [InlineData(0, -1)]
        [InlineData(0, 2)]
        public void Validate_RejectsRoadPathOutsideDimensions(int x, int y)
        {
            var road = new Road(Guid.NewGuid(), FirstId, SecondId,
                new[] { new GridPosition(0, 0), new GridPosition(x, y), new GridPosition(1, 0) });
            var snapshot = Snapshot(new[] { Placement(FirstId, 0, 0), Placement(SecondId, 1, 0) }, new[] { road });

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(snapshot));
        }

        [Fact]
        public void Road_WithoutPath_CanBeConstructedForProjectionInput()
        {
            var road = new Road(Guid.NewGuid(), FirstId, SecondId);

            Assert.Empty(road.GridPath);
        }

        [Fact]
        public void Validate_RejectsRoadWithUnknownEndpoint()
        {
            var road = new Road(Guid.NewGuid(), FirstId, Guid.NewGuid(),
                new[] { new GridPosition(0, 0), new GridPosition(1, 0) });
            var snapshot = Snapshot(new[] { Placement(FirstId, 0, 0) }, new[] { road });

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(snapshot));
        }

        [Fact]
        public void Validate_RejectsPlacementOutsideDimensions()
        {
            var snapshot = Snapshot(new[] { Placement(FirstId, 2, 0) }, Array.Empty<Road>());

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(snapshot));
        }

        [Fact]
        public void Validate_RejectsDuplicatePlacementElementIdOrPosition()
        {
            var duplicateId = Snapshot(new[] { Placement(FirstId, 0, 0), Placement(FirstId, 1, 0) }, Array.Empty<Road>());
            var duplicatePosition = Snapshot(new[] { Placement(FirstId, 0, 0), Placement(SecondId, 0, 0) }, Array.Empty<Road>());

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(duplicateId));
            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(duplicatePosition));
        }

        [Fact]
        public void Validate_RejectsDuplicateRoadId()
        {
            var roadId = Guid.NewGuid();
            var roads = new[]
            {
                new Road(roadId, FirstId, SecondId, new[] { new GridPosition(0, 0) }),
                new Road(roadId, FirstId, SecondId, new[] { new GridPosition(1, 0) })
            };
            var snapshot = Snapshot(new[] { Placement(FirstId, 0, 0), Placement(SecondId, 1, 0) }, roads);

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(snapshot));
        }

        [Fact]
        public void Validate_ValidSnapshot_DoesNotThrow()
        {
            var snapshot = Snapshot(new[] { Placement(FirstId, 0, 0), Placement(SecondId, 1, 0) },
                new[] { new Road(Guid.NewGuid(), FirstId, SecondId, new[] { new GridPosition(0, 0), new GridPosition(1, 0) }) });

            MapSnapshotValidator.Validate(snapshot);
        }

        [Fact]
        public void Validate_RejectsUnknownAlgorithmVersion()
        {
            var snapshot = new MapSnapshot(MapId, new MapDimensions(2, 2), 42, "v2",
                new[] { new BiomeId("plains") }, new[] { Placement(FirstId, 0, 0) }, Array.Empty<Road>());

            Assert.Throws<MapValidationException>(() => MapSnapshotValidator.Validate(snapshot));
        }

        [Fact]
        public void GenerationOptions_RejectsBlankAlgorithmVersion()
        {
            Assert.Throws<ArgumentException>(() => new MapGenerationOptions(
                new MapDimensions(2, 2), 1, new[] { new BiomeId("plains") }, " ", MapId));
        }

        [Fact]
        public void GenerationOptions_RejectsUnsupportedAlgorithmVersion()
        {
            Assert.Throws<ArgumentException>(() => new MapGenerationOptions(
                new MapDimensions(2, 2), 1, new[] { new BiomeId("plains") }, "v2", MapId));
        }

        private static LocationPlacement Placement(Guid id, int x, int y) =>
            new LocationPlacement(id, new GridPosition(x, y), new BiomeId("plains"));

        private static MapCell Cell(int x, int y) =>
            new MapCell(new GridPosition(x, y), 0.5f, new BiomeId("plains"));

        private static MapCell[] CompleteCells() => new[] { Cell(0, 0), Cell(1, 0), Cell(0, 1), Cell(1, 1) };

        private static MapSnapshot CompleteSnapshot(IReadOnlyList<MapCell>? cells = null,
            IReadOnlyList<LocationPlacement>? placements = null, IReadOnlyList<Road>? roads = null) =>
            new MapSnapshot(MapId, new MapDimensions(2, 2), 42, "v1", new[] { new BiomeId("plains") },
                cells ?? CompleteCells(), placements ?? Array.Empty<LocationPlacement>(), roads ?? Array.Empty<Road>());

        private static MapSnapshot Snapshot(IReadOnlyList<LocationPlacement> placements, IReadOnlyList<Road> roads) =>
            new MapSnapshot(MapId, new MapDimensions(2, 2), 42, "v1", new[] { new BiomeId("plains") }, placements, roads);
    }
}
