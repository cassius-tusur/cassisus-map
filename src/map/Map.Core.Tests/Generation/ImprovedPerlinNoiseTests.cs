using System;
using Map.Core.Generation;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class ImprovedPerlinNoiseTests
    {
        [Fact]
        public void Sample_SameSeedAndCoordinates_IsStable()
        {
            var left = new ImprovedPerlinNoise(123).Sample(3.25, -7.5);
            var right = new ImprovedPerlinNoise(123).Sample(3.25, -7.5);

            Assert.Equal(left, right, 12);
        }

        [Fact]
        public void Sample_IsNormalizedToUnitInterval()
        {
            var noise = new ImprovedPerlinNoise(123);

            for (var x = -4.0; x <= 4.0; x += 0.37)
            for (var y = -4.0; y <= 4.0; y += 0.41)
            {
                var value = noise.Sample(x, y);
                Assert.InRange(value, 0d, 1d);
            }
        }

        [Fact]
        public void Sample_DifferentSeedsProduceDifferentField()
        {
            var first = new ImprovedPerlinNoise(1).Sample(3.25, -7.5);
            var second = new ImprovedPerlinNoise(2).Sample(3.25, -7.5);

            Assert.NotEqual(first, second);
        }
    }
}
