using System.Security.Cryptography;
using System.Text;
using SimpleBase;

namespace GoogleFitOnFhir
{
    public static class Utility
    {
        public static string Base58String(string stringToBase58)
        {
            byte[] emailToBase58 = Encoding.ASCII.GetBytes(stringToBase58);
            string base58Email = Base58.Bitcoin.Encode(emailToBase58);
            return base58Email.ToString();
        }
    }
}