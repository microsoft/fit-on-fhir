using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class UtilityTests
    {
        [Theory]
        [InlineData("test", "098F6BCD4621D373CADE4E832627B4F6")]
        [InlineData("coolpersonemail@gmail.com", "D4AA96AE8E9440B5B5A2CC9811594592")]
        public void TestMD5StringMD5sTheString(string stringToMD5, string md5d)
        {
            Assert.Equal(GoogleFitOnFhir.Utility.MD5String(stringToMD5), md5d);
        }

        [Theory]
        [InlineData("test", "3yZe7d")]
        [InlineData("coolpersonemail@gmail.com", "h1cvxtdGQs6hDFxxgWLDebuFKpQ11pNHR6")]
        public void TestBase58StringBase58sTheString(string stringToBase58, string base58d)
        {
            Assert.Equal(GoogleFitOnFhir.Utility.Base58String(stringToBase58), base58d);
        }
    }
}