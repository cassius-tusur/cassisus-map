using System;
using Xunit;

namespace Map.Core.Tests
{
    public class ArchitectureTests
    {
        [Fact]
        public void CoreReferencesBaseClassesButNotUnity()
        {
            var refs = typeof(MapCoreAssemblyMarker).Assembly.GetReferencedAssemblies();
            Assert.Contains(refs, item => item.Name == "BaseClasses");
            Assert.DoesNotContain(refs, item => item.Name!.StartsWith("Unity", StringComparison.Ordinal));
        }
    }
}
