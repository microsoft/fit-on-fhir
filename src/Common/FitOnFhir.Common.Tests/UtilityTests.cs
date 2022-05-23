// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using FitOnFhir.GoogleFit;
using Xunit;

namespace FitOnFhir.Common.Tests
{
    public class UtilityTests
    {
        [Theory]
        [InlineData("test", "3yZe7d")]
        [InlineData("coolpersonemail@gmail.com", "h1cvxtdGQs6hDFxxgWLDebuFKpQ11pNHR6")]
        public void TestBase58StringBase58sTheString(string stringToBase58, string base58d)
        {
            Assert.Equal(Utility.Base58String(stringToBase58), base58d);
        }
    }
}