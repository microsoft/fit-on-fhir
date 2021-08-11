using System;
using System.Security.Cryptography;
using System.Text;

namespace GoogleFitOnFhir
{
    public static class Utility
    {
        public static string Base64String(string stringToBase64)
        {
            byte[] encodedEmail = System.Text.Encoding.UTF8.GetBytes(stringToBase64);
            string base64Email = Convert.ToBase64String(encodedEmail).Replace("=", string.Empty);
            return base64Email;
        }
    }
}