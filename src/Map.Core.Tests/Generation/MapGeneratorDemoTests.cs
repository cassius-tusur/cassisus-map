using System;
using System.Diagnostics;
using System.IO;
using Map.Core.Serialization;
using Map.Core.Validation;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class MapGeneratorDemoTests
    {
        [Fact]
        public void Demo_GeneratesPersistsAndRestoresACompleteV1Snapshot()
        {
            var outputPath = Path.Combine(Path.GetTempPath(), $"map-core-demo-{Guid.NewGuid():N}.msgpack");
            try
            {
                var output = RunDemo(outputPath, out var error);

                Assert.True(File.Exists(outputPath), error);
                var payload = File.ReadAllBytes(outputPath);
                Assert.NotEmpty(payload);

                var restored = new MapSnapshotSerializer().Deserialize(payload);
                MapSnapshotValidator.Validate(restored);
                Assert.Equal("v1", restored.AlgorithmVersion);
                Assert.Equal(restored.Dimensions.Width * restored.Dimensions.Height, restored.Cells.Count);
                Assert.Equal(3, restored.Placements.Count);
                Assert.NotEmpty(restored.Roads);
                Assert.Contains("MessagePack", output);
                Assert.Contains("Restored snapshot passed validation.", output);
                Assert.Contains("Restored snapshot matches generated snapshot.", output);
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        private static string RunDemo(string outputPath, out string error)
        {
            var demoAssembly = Path.Combine(AppContext.BaseDirectory, "Map.Core.Demo.dll");
            var startInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(demoAssembly);
            startInfo.ArgumentList.Add(outputPath);

            using var process = Process.Start(startInfo)!;
            var output = process.StandardOutput.ReadToEnd();
            error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Assert.True(process.ExitCode == 0, error);
            return output;
        }
    }
}
