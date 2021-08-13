using System.Security.Cryptography;
using System.Text;
using SimpleBase;

namespace GoogleFitOnFhir
{
    public static class Utility
    {
        public static string MD5String(string stringToMD5)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] encodedEmail = new UTF8Encoding().GetBytes(stringToMD5);
                byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encodedEmail);
                StringBuilder md5Email = new StringBuilder();

                for (int i = 0; i < hash.Length; i++)
                {
                    md5Email.Append(hash[i].ToString("X2"));
                }

                return md5Email.ToString();
            }
        }

        public static string Base58String(string stringToBase58)
        {
            byte[] emailToBase58 = Encoding.ASCII.GetBytes(stringToBase58);
            string base58Email = Base58.Bitcoin.Encode(emailToBase58);
            return base58Email.ToString();
        }
    }
}