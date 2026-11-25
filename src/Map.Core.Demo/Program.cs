using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaseClasses.Enum;
using BaseClasses.Model;
using BaseClasses.Services.Binds;
using Map.Core.Generation;
using Map.Core.Model;
using Map.Core.Serialization;
using Map.Core.Validation;

var outputPath = ResolveOutputPath(args);
var plot = new Plot();
var locations = new[]
{
    new Element(ElemType.Location, "Port Aurora"),
    new Element(ElemType.Location, "Whispering Pines"),
    new Element(ElemType.Location, "Old Watchtower")
};

foreach (var location in locations)
    plot.Add(location);

plot.Bind(locations[0], locations[1], BaseRelationKeys.Passage, true);
plot.Bind(locations[1], locations[2], BaseRelationKeys.Passage, true);

var options = new MapGenerationOptions(
    Guid.Parse("11111111-1111-1111-1111-111111111111"),
    new MapDimensions(6, 4),
    seed: 42,
    biomes: new[] { new BiomeId("coast"), new BiomeId("forest"), new BiomeId("plains") },
    algorithmVersion: MapAlgorithmVersions.V1);

var snapshot = new MapGenerator().Generate(plot, options).Snapshot;
MapSnapshotValidator.Validate(snapshot);

var serializer = new MapSnapshotSerializer();
var payload = serializer.Serialize(snapshot);
File.WriteAllBytes(outputPath, payload);

var restoredSnapshot = serializer.Deserialize(File.ReadAllBytes(outputPath));
MapSnapshotValidator.Validate(restoredSnapshot);
if (!SnapshotsAreEquivalent(snapshot, restoredSnapshot))
    throw new InvalidOperationException("Restored snapshot does not match the generated snapshot.");

var namesById = locations.ToDictionary(location => location.Id, location => location.Name);

Console.WriteLine($"Saved MessagePack v1 snapshot to {outputPath} ({payload.Length} bytes).");
Console.WriteLine("Restored snapshot passed validation.");
Console.WriteLine("Restored snapshot matches generated snapshot.");
Console.WriteLine($"Map {snapshot.Dimensions.Width}x{snapshot.Dimensions.Height}, seed {snapshot.Seed}");
Console.WriteLine($"Cells: {snapshot.Cells.Count}; placements: {snapshot.Placements.Count}; roads: {snapshot.Roads.Count}");
foreach (var placement in snapshot.Placements.OrderBy(placement => namesById[placement.ElementId], StringComparer.Ordinal))
    Console.WriteLine($"{namesById[placement.ElementId]}: ({placement.Position.X}, {placement.Position.Y}), {placement.Biome.Value}");

static string ResolveOutputPath(string[] arguments)
{
    if (arguments.Length > 1)
        throw new ArgumentException("Usage: Map.Core.Demo [output-path]");

    if (arguments.Length == 1)
    {
        if (string.IsNullOrWhiteSpace(arguments[0]))
            throw new ArgumentException("Output path cannot be blank.", nameof(arguments));
        return Path.GetFullPath(arguments[0]);
    }

    return Path.Combine(Path.GetTempPath(), "map-core-demo-v1.msgpack");
}

static bool SnapshotsAreEquivalent(MapSnapshot expected, MapSnapshot actual)
{
    return expected.MapId == actual.MapId &&
           expected.Dimensions == actual.Dimensions &&
           expected.Seed == actual.Seed &&
           expected.AlgorithmVersion == actual.AlgorithmVersion &&
           expected.Biomes.SequenceEqual(actual.Biomes) &&
           expected.Cells.SequenceEqual(actual.Cells) &&
           expected.Placements.SequenceEqual(actual.Placements) &&
           RoadsAreEquivalent(expected.Roads, actual.Roads);
}

static bool RoadsAreEquivalent(IReadOnlyList<Road> expected, IReadOnlyList<Road> actual)
{
    if (expected.Count != actual.Count)
        return false;

    for (var index = 0; index < expected.Count; index++)
    {
        var expectedRoad = expected[index];
        var actualRoad = actual[index];
        if (expectedRoad.RoadId != actualRoad.RoadId ||
            expectedRoad.FirstEndpointId != actualRoad.FirstEndpointId ||
            expectedRoad.SecondEndpointId != actualRoad.SecondEndpointId ||
            !expectedRoad.GridPath.SequenceEqual(actualRoad.GridPath))
            return false;
    }

    return true;
}
