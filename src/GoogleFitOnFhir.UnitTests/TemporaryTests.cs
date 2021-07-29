using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class TemporaryTests
    {
        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(2, 7, 9)]
        public void Test1(int x, int y, int z)
        {
            Assert.Equal(new Temporary().Add(x, y), z);
        }
    }
}
