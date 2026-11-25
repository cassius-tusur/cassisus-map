using Map.Core.Model;
using MessagePack;
using Xunit;

namespace Map.Core.Tests.Serialization
{
    public class MapParameterMessagePackTests
    {
        [Fact]
        public void GridPositionRoundTripsWithStandardResolver()
        {
            var value = new GridPosition(2, 3);
            Assert.Equal(value, MessagePackSerializer.Deserialize<GridPosition>(MessagePackSerializer.Serialize(value)));
        }

        [Fact]
        public void BiomeIdRoundTripsWithStandardResolver()
        {
            var value = new BiomeId("coast");
            Assert.Equal(value, MessagePackSerializer.Deserialize<BiomeId>(MessagePackSerializer.Serialize(value)));
        }
    }
}
