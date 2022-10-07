// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Google.Apis.Fitness.v1.Data;
using Microsoft.Health.Tests.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Tests
{
    public class DeviceContentTemplateGeneratorTests
    {
        private readonly DeviceContentTemplateGenerator _generator;

        public DeviceContentTemplateGeneratorTests()
        {
            _generator = new DeviceContentTemplateGenerator();
        }

        [Theory]
        [FileData(@"TestInput/DataSource.json", @"Expected/DeviceContent.json")]
        public async Task GivenValidDataSource_WhenGenerateTemplatesCalled_CalculatedFunctionContentTemplateGenerated(string inputJson, string expectedJson)
        {
            DataSource dataSource = JsonConvert.DeserializeObject<DataSource>(inputJson);
            JObject expected = JObject.Parse(expectedJson);

            JArray deviceContent = await _generator.GenerateTemplates(dataSource, CancellationToken.None);

            Assert.Single(deviceContent);
            Assert.True(JToken.DeepEquals(expected, deviceContent.FirstOrDefault()));
        }
    }
}
