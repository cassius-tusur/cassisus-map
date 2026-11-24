using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Map.Core.Tests.Architecture
{
    public class LegacyIsolationTests
    {
        [Fact]
        public void CoreAndDemoSourcesContainNoUnityOrLegacyPersistenceReferences()
        {
            Assert.DoesNotContain(ReadAllCoreAndDemoSources(), text => text.Contains("UnityEngine"));
            Assert.DoesNotContain(ReadAllCoreAndDemoSources(), text => text.Contains("Assets/Scripts"));
            Assert.DoesNotContain(ReadAllCoreAndDemoSources(), text => text.Contains("System.Text.Json"));
            Assert.DoesNotContain(ReadAllCoreAndDemoSources(), text => text.Contains("Newtonsoft.Json"));
        }

        [Fact]
        public void CoreSourceEnumerationExcludesBuildOutputDirectories()
        {
            var sourcePaths = FindCoreAndDemoSourcePaths().ToArray();

            Assert.NotEmpty(sourcePaths);
            Assert.All(sourcePaths, path =>
            {
                var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Assert.DoesNotContain(segments, segment =>
                    string.Equals(segment, "bin", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(segment, "obj", StringComparison.OrdinalIgnoreCase));
            });
        }

        private static IEnumerable<string> ReadAllCoreAndDemoSources()
        {
            return FindCoreAndDemoSourcePaths().Select(File.ReadAllText);
        }

        private static IEnumerable<string> FindCoreAndDemoSourcePaths()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (!(directory is null))
            {
                var coreDirectory = Path.Combine(directory.FullName, "Map.Core");
                if (Directory.Exists(coreDirectory))
                {
                    var demoDirectory = Path.Combine(directory.FullName, "Map.Core.Demo");
                    return Directory.Exists(demoDirectory)
                        ? EnumerateProductionSources(coreDirectory).Concat(EnumerateProductionSources(demoDirectory))
                        : EnumerateProductionSources(coreDirectory);
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                $"Could not locate the Map.Core source directory from '{AppContext.BaseDirectory}'.");
        }

        private static IEnumerable<string> EnumerateProductionSources(string root)
        {
            var pending = new Stack<DirectoryInfo>();
            pending.Push(new DirectoryInfo(root));

            while (pending.Count > 0)
            {
                var directory = pending.Pop();

                foreach (var sourceFile in directory.EnumerateFiles("*.cs", SearchOption.TopDirectoryOnly))
                {
                    yield return sourceFile.FullName;
                }

                foreach (var childDirectory in directory.EnumerateDirectories())
                {
                    if (!IsBuildOutputDirectory(childDirectory))
                    {
                        pending.Push(childDirectory);
                    }
                }
            }
        }

        private static bool IsBuildOutputDirectory(DirectoryInfo directory)
        {
            return string.Equals(directory.Name, "bin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(directory.Name, "obj", StringComparison.OrdinalIgnoreCase);
        }
    }
}
