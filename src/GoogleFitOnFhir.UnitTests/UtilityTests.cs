using Xunit;

namespace GoogleFitOnFhir.UnitTests
{
    public class UtilityTests
    {
        [Theory]
        [InlineData("test", "dGVzdA")]
        [InlineData("coolpersonemail@gmail.com", "Y29vbHBlcnNvbmVtYWlsQGdtYWlsLmNvbQ")]
        public void TestBase64StringBase64sTheString(string stringToBase64, string base64d)
        {
            Assert.Equal(GoogleFitOnFhir.Utility.Base64String(stringToBase64), base64d);
        }
    }
}