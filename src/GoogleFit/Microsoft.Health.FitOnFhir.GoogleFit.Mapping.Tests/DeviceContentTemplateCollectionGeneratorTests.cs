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
    public class DeviceContentTemplateCollectionGeneratorTests
    {
        [Theory]
        [FileData(@"TestInput/DataSources.json", @"Expected/UnfilteredCollection.json")]
        public async Task GivenValidDataSourcesWithNoFilter_WhenGenerateTemplateCollectionCalled_CalculatedFunctionContentTemplateCollectionGenerated(string inputJson, string expectedJson)
        {
            var generator = new DeviceContentTemplateCollectionGenerator();

            IEnumerable<DataSource> template = JsonConvert.DeserializeObject<IEnumerable<DataSource>>(inputJson);
            JObject expected = JObject.Parse(expectedJson);

            JObject templateCollection = await generator.GenerateTemplateCollection(template, CancellationToken.None);

            Assert.True(JToken.DeepEquals(expected, templateCollection));
        }

        [Theory]
        [FileData(@"TestInput/DataSources.json", @"Expected/FilteredCollection.json")]
        public async Task GivenValidDataSourcesWithFilter_WhenGenerateTemplateCollectionCalled_CalculatedFunctionContentTemplateCollectionGenerated(string inputJson, string expectedJson)
        {
            var generator = new DeviceContentTemplateCollectionGenerator("com\\.google\\.activity\\.segment");

            IEnumerable<DataSource> template = JsonConvert.DeserializeObject<IEnumerable<DataSource>>(inputJson);
            JObject expected = JObject.Parse(expectedJson);

            JObject templateCollection = await generator.GenerateTemplateCollection(template, CancellationToken.None);

            Assert.True(JToken.DeepEquals(expected, templateCollection));
        }
    }
}
