using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class UtilityTests
    {
        [Theory]
        [InlineData("test", "3yZe7d")]
        [InlineData("coolpersonemail@gmail.com", "h1cvxtdGQs6hDFxxgWLDebuFKpQ11pNHR6")]
        public void TestBase58StringBase58sTheString(string stringToBase58, string base58d)
        {
            Assert.Equal(GoogleFitOnFhir.Utility.Base58String(stringToBase58), base58d);
        }
    }
}