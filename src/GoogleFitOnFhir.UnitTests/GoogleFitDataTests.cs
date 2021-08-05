using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class GoogleFitDataTests
    {
        [Fact]
        public void TestGoogleFitDataConstructs()
        {
            GoogleFitData gfit = new GoogleFitData("accessTokenHere");
            Assert.IsType<GoogleFitData>(gfit);
        }
    }
}
