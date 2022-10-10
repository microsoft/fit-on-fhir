// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Ingest.Template;
using Microsoft.Health.Fhir.Ingest.Template.Generator;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping
{
    public class FhirMappingTemplateCollectionGenerator : TemplateCollectionGenerator<CalculatedFunctionContentTemplate>
    {
        private static CodeValueFhirTemplateGenerator _generator = new CodeValueFhirTemplateGenerator();

        protected override bool RequireUniqueTemplateTypeNames => true;

        protected override TemplateCollectionType CollectionType => TemplateCollectionType.CollectionFhir;

        public override async Task<JArray> GetTemplates(CalculatedFunctionContentTemplate model, CancellationToken cancellationToken)
        {
            return await _generator.GenerateTemplates(model, cancellationToken);
        }
    }
}
