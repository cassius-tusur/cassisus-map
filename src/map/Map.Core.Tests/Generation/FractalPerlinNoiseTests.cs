using Map.Core.Generation;
using Map.Core.Model;
using Xunit;

namespace Map.Core.Tests.Generation
{
    public class FractalPerlinNoiseTests
    {
        private static readonly NoiseFieldOptions FixtureOptions =
            new NoiseFieldOptions(24f, 4, 0.5f, 2f, 17.25f, -31.5f);

        [Fact]
        public void Generate_FixedSeedAndOptions_ProducesStableNormalizedFirstRow()
        {
            var field = FractalPerlinNoise.Generate(new MapDimensions(4, 3), 12345, FixtureOptions);

            Assert.Equal(new[] { 0.281258017f, 0.524304032f, 0.7518785f, 1f },
                new[] { field[0], field[1], field[2], field[3] });
            Assert.All(field, value => Assert.InRange(value, 0f, 1f));
        }

        [Fact]
        public void Generate_RepeatsExactlyForSameInputs()
        {
            var dimensions = new MapDimensions(5, 4);

            var first = FractalPerlinNoise.Generate(dimensions, 9876, FixtureOptions);
            var second = FractalPerlinNoise.Generate(dimensions, 9876, FixtureOptions);

            Assert.Equal(first, second);
        }

        [Fact]
        public void Generate_ReturnsCellsInRowMajorOrder()
        {
            var dimensions = new MapDimensions(3, 2);
            var field = FractalPerlinNoise.Generate(dimensions, 42, FixtureOptions);

            Assert.Equal(dimensions.Width * dimensions.Height, field.Length);
            Assert.Equal(0.78910172f, field[0]);
            Assert.Equal(0.34817487f, field[1]);
            Assert.Equal(0.140216053f, field[2]);
            Assert.Equal(1f, field[3]);
            Assert.Equal(0.37048018f, field[4]);
        }

        [Fact]
        public void Generate_OneByOneField_ReturnsNormalizedZeroForConstantRange()
        {
            var field = FractalPerlinNoise.Generate(new MapDimensions(1, 1), 42, FixtureOptions);

            Assert.Equal(new[] { 0f }, field);
        }
    }
}
