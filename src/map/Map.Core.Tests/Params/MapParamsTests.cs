using System;
using Map.Core.Model;
using Map.Core.Params;
using Xunit;

namespace Map.Core.Tests.Params
{
    public class MapParamsTests
    {
        [Fact]
        public void ForMapId_UsesDedicatedTypedNamespace()
        {
            var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var keys = MapParams.For(id);

            Assert.Equal("Map/11111111-1111-1111-1111-111111111111", keys.Position.Namespace);
            Assert.Equal(typeof(GridPosition), keys.Position.ValueType);
        }

        [Fact]
        public void ForMapId_RejectsEmptyId()
        {
            Assert.Throws<ArgumentException>(() => MapParams.For(Guid.Empty));
        }

        [Fact]
        public void ForMapId_ExposesNamedKeysForEveryMapParameter()
        {
            var keys = MapParams.For(Guid.NewGuid());

            Assert.Equal("Position", keys.Position.Name);
            Assert.Equal("Biome", keys.Biome.Name);
            Assert.Equal("MapId", keys.MapId.Name);
            Assert.Equal("Seed", keys.Seed.Name);
            Assert.Equal("AlgorithmVersion", keys.AlgorithmVersion.Name);
        }
    }
}
