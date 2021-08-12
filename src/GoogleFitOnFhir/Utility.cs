using System.Security.Cryptography;
using System.Text;

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
    }
}