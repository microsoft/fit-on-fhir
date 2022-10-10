// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Ingest.Template;
using Microsoft.Health.Tests.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Tests
{
    public class CodeValueFhirTemplateGeneratorTests
    {
        private readonly CodeValueFhirTemplateGenerator _generator;

        public CodeValueFhirTemplateGeneratorTests()
        {
            _generator = new CodeValueFhirTemplateGenerator();
        }

        [Theory]
        [FileData(@"TestInput/CalculatedFunctionContentTemplate.json", @"Expected/FhirMapping.json")]
        public async Task GivenValidCalculatedFunctionContentTemplate_WhenGenerateTemplatesCalled_CodeValueFhirTemplateGenerated(string inputJson, string expectedJson)
        {
            CalculatedFunctionContentTemplate template = JsonConvert.DeserializeObject<CalculatedFunctionContentTemplate>(inputJson);
            JObject expected = JObject.Parse(expectedJson);

            JArray fhirMapping = await _generator.GenerateTemplates(template, CancellationToken.None);

            Assert.Single(fhirMapping);
            Assert.True(JToken.DeepEquals(expected, fhirMapping.FirstOrDefault()));
        }
    }
}
