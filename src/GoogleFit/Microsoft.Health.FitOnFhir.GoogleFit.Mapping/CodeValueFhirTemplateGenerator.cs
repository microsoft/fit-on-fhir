// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Health.Fhir.Ingest.Template;
using Microsoft.Health.Fhir.Ingest.Template.Generator;
using Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Extensions;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping
{
    public class CodeValueFhirTemplateGenerator : CodeValueFhirTemplateGenerator<CalculatedFunctionContentTemplate>
    {
        public override Task<IEnumerable<string>> GetTypeNames(CalculatedFunctionContentTemplate model, CancellationToken cancellationToken)
        {
            if (model != null)
            {
                return Task.FromResult<IEnumerable<string>>(new List<string>() { model.TypeName });
            }

            return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
        }

        public override Task<IList<FhirCode>> GetCodes(CalculatedFunctionContentTemplate model, CancellationToken cancellationToken)
        {
            return Task.FromResult<IList<FhirCode>>(model.GetFhirCodes());
        }

        public override Task<FhirValueType> GetValue(CalculatedFunctionContentTemplate model, CancellationToken cancellationToken)
        {
            // If the model has just one value, the FHIR Observation will contain a single value (no Components).
            IList<FhirValueType> values = model.GetFhirValues();

            if (values.Count == 1)
            {
                return Task.FromResult(values[0]);
            }

            return Task.FromResult<FhirValueType>(null);
        }

        public override Task<IList<CodeValueMapping>> GetComponents(CalculatedFunctionContentTemplate model, CancellationToken cancellationToken)
        {
            // If the model has multiple values, the FHIR Observation will contain Components, rather than a single value).
            IList<FhirValueType> values = model.GetFhirValues();
            string codePrefix = model.GetCode();

            if (values.Count > 1)
            {
                IList<CodeValueMapping> components = new List<CodeValueMapping>();

                foreach (var value in values)
                {
                    IList<FhirCode> defaultCodes = value.GetFhirCodes(codePrefix);

                    components.Add(new CodeValueMapping()
                    {
                        Codes = value.GetFhirCodes(codePrefix),
                        Value = value,
                    });
                }

                return Task.FromResult(components);
            }

            return Task.FromResult<IList<CodeValueMapping>>(null);
        }
    }
}
