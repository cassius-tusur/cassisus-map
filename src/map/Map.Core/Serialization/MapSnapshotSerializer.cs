using System;
using System.Collections.Generic;
using Map.Core.Model;
using Map.Core.Serialization.Wire;
using Map.Core.Validation;
using MessagePack;

namespace Map.Core.Serialization
{
    /// <summary>
    /// Serializes and validates map snapshots using the versioned MessagePack wire format.
    /// </summary>
    /// <seealso cref="MapSnapshot"/>
    public sealed class MapSnapshotSerializer
    {
        private const int FormatVersion = 1;
        private static readonly MessagePackSerializerOptions SerializerOptions =
            MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);

        /// <summary>
        /// Serializes the supplied value.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <returns>The MessagePack payload containing the snapshot.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        public byte[] Serialize(MapSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            MapSnapshotValidator.Validate(snapshot);
            return MessagePackSerializer.Serialize(new MapSnapshotEnvelopeDto
            {
                FormatVersion = FormatVersion,
                Snapshot = ToDto(snapshot)
            }, SerializerOptions);
        }

        /// <summary>
        /// Deserializes the supplied value.
        /// </summary>
        /// <param name="payload">The serialized snapshot payload.</param>
        /// <returns>The validated map snapshot reconstructed from the payload.</returns>
        /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null"/>.</exception>
        /// <exception cref="MapValidationException">Thrown when the map snapshot or projection is invalid.</exception>
        public MapSnapshot Deserialize(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Length == 0)
                throw new MapValidationException("Snapshot payload cannot be empty.");

            try
            {
                var envelope = MessagePackSerializer.Deserialize<MapSnapshotEnvelopeDto>(payload, SerializerOptions,
                    out var bytesRead);
                if (bytesRead != payload.Length)
                    throw new MapValidationException("Snapshot payload contains trailing bytes.");
                if (envelope == null) throw new MapValidationException("Snapshot payload must contain an envelope.");
                if (envelope.FormatVersion != FormatVersion)
                    throw new MapValidationException($"Unsupported snapshot format version '{envelope.FormatVersion}'.");
                if (envelope.Snapshot == null) throw new MapValidationException("Snapshot envelope must contain a snapshot.");
                var snapshot = FromDto(envelope.Snapshot);
                MapSnapshotValidator.Validate(snapshot);
                return snapshot;
            }
            catch (MapValidationException) { throw; }
            catch (Exception error) when (error is MessagePackSerializationException || error is ArgumentException ||
                                          error is OverflowException)
            {
                throw new MapValidationException($"Snapshot payload is malformed: {error.Message}");
            }
        }

        /// <summary>
        /// Converts a map snapshot to its wire-format DTO.
        /// </summary>
        /// <param name="snapshot">The map snapshot to process.</param>
        /// <returns>The wire-format DTO.</returns>
        private static MapSnapshotDto ToDto(MapSnapshot snapshot)
        {
            var dto = new MapSnapshotDto
            {
                MapId = snapshot.MapId, Width = snapshot.Dimensions.Width, Height = snapshot.Dimensions.Height,
                Seed = snapshot.Seed, AlgorithmVersion = snapshot.AlgorithmVersion,
                Biomes = new List<string>(), Cells = new List<MapCellDto>(), Placements = new List<LocationPlacementDto>(),
                Roads = new List<RoadDto>()
            };
            foreach (var biome in snapshot.Biomes) dto.Biomes.Add(biome.Value);
            foreach (var cell in snapshot.Cells)
                dto.Cells.Add(new MapCellDto { X = cell.Position.X, Y = cell.Position.Y,
                    TerrainHeight = cell.TerrainHeight, Biome = cell.Biome.Value });
            foreach (var placement in snapshot.Placements)
                dto.Placements.Add(new LocationPlacementDto { ElementId = placement.ElementId, X = placement.Position.X,
                    Y = placement.Position.Y, Biome = placement.Biome.Value, Label = placement.Label });
            foreach (var road in snapshot.Roads)
            {
                var roadDto = new RoadDto { RoadId = road.RoadId, FirstEndpointId = road.FirstEndpointId,
                    SecondEndpointId = road.SecondEndpointId, Path = new List<GridPositionDto>() };
                foreach (var position in road.GridPath)
                    roadDto.Path.Add(new GridPositionDto { X = position.X, Y = position.Y });
                dto.Roads.Add(roadDto);
            }
            return dto;
        }

        /// <summary>
        /// Reconstructs a map snapshot from its wire-format DTO.
        /// </summary>
        /// <param name="dto">The dto value.</param>
        /// <returns>The reconstructed map snapshot.</returns>
        /// <exception cref="MapValidationException">Thrown when the map snapshot or projection is invalid.</exception>
        private static MapSnapshot FromDto(MapSnapshotDto dto)
        {
            if (dto.AlgorithmVersion == null || dto.Biomes == null || dto.Cells == null || dto.Placements == null ||
                dto.Roads == null)
                throw new MapValidationException("Snapshot fields and collections cannot be null.");

            var biomes = new List<BiomeId>();
            foreach (var biome in dto.Biomes) biomes.Add(new BiomeId(biome));
            var cells = new List<MapCell>();
            foreach (var cell in dto.Cells)
            {
                if (cell == null) throw new MapValidationException("Snapshot cells cannot contain null entries.");
                cells.Add(new MapCell(new GridPosition(cell.X, cell.Y), cell.TerrainHeight, new BiomeId(cell.Biome!)));
            }
            var placements = new List<LocationPlacement>();
            foreach (var placement in dto.Placements)
            {
                if (placement == null) throw new MapValidationException("Snapshot placements cannot contain null entries.");
                placements.Add(new LocationPlacement(placement.ElementId, new GridPosition(placement.X, placement.Y),
                    new BiomeId(placement.Biome!), placement.Label));
            }
            var roads = new List<Road>();
            foreach (var road in dto.Roads)
            {
                if (road == null || road.Path == null) throw new MapValidationException("Snapshot roads and road paths cannot be null.");
                var path = new List<GridPosition>();
                foreach (var position in road.Path)
                {
                    if (position == null) throw new MapValidationException("Road paths cannot contain null entries.");
                    path.Add(new GridPosition(position.X, position.Y));
                }
                roads.Add(new Road(road.RoadId, road.FirstEndpointId, road.SecondEndpointId, path));
            }
            return new MapSnapshot(dto.MapId, new MapDimensions(dto.Width, dto.Height), dto.Seed,
                dto.AlgorithmVersion, biomes, cells, placements, roads);
        }
    }
}
