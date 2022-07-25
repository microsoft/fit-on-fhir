// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using SimpleBase;

namespace Microsoft.Health.FitOnFhir.GoogleFit
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