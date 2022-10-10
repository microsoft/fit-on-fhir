// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using EnsureThat;
using Microsoft.Health.Fhir.Ingest.Template;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Extensions
{
    public static class CalculatedFunctionContentTemplateExtensions
    {
        private static IDictionary<string, Type> _valueTypeMap = new Dictionary<string, Type>()
        {
            { MappingConstants.StringValue, typeof(StringFhirValueType) },
            { MappingConstants.IntegerValue, typeof(QuantityFhirValueType) },
            { MappingConstants.FloatPointValue, typeof(QuantityFhirValueType) },
        };

        public static string GetTypeNameForFile(this CalculatedFunctionContentTemplate template)
        {
            EnsureArg.IsNotNullOrWhiteSpace(template?.TypeName, nameof(template.TypeName));

            return Regex.Replace(template.TypeName, "[<.: ]", string.Empty);
        }

        public static string GetCode(this CalculatedFunctionContentTemplate template)
        {
            EnsureArg.IsNotNullOrWhiteSpace(template?.TypeName, nameof(template.TypeName));

            string[] components = template.TypeName.Split(":");

            if (components.Length > 1)
            {
                return components[1];
            }

            return default;
        }

        public static IList<FhirCode> GetFhirCodes(this CalculatedFunctionContentTemplate template)
        {
            return new List<FhirCode>
            {
                new FhirCode
                {
                    Code = GetCode(template),
                },
            };
        }

        public static IList<FhirValueType> GetFhirValues(this CalculatedFunctionContentTemplate template)
        {
            var values = new List<FhirValueType>();

            if (template?.Values != null)
            {
                foreach (CalculatedFunctionValueExpression value in template.Values)
                {
                    string valueType = value.ValueExpression.Value.Split(".").LastOrDefault();

                    if (valueType != null)
                    {
                        // Default type will be string.
                        _valueTypeMap.TryGetValue(valueType, out var type);
                        type = type ?? typeof(StringFhirValueType);
                        FhirValueType fhirValue = Activator.CreateInstance(type) as FhirValueType;
                        fhirValue.ValueType = type.Name.Substring(0, type.Name.Length - typeof(FhirValueType).Name.Length);
                        fhirValue.ValueName = value.ValueName;

                        values.Add(fhirValue);
                    }
                }
            }

            return values;
        }
    }
}
