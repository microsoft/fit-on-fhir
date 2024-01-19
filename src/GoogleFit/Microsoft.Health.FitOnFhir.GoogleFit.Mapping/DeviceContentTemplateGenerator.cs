// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;
using EnsureThat;
using Google.Apis.Fitness.v1.Data;
using Microsoft.Health.Fhir.Ingest.Template;
using Microsoft.Health.Fhir.Ingest.Template.Generator;
using Microsoft.Health.FitOnFhir.GoogleFit.Common;
using Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Enums;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping
{
    public class DeviceContentTemplateGenerator : CalculatedContentTemplateGenerator<DataSource>
    {
        private List<string> _includedDataStreamExpressions = new List<string>();

        /// <summary>
        /// A format string that allows the TypeMatchExpression to match a specific DataSource.DataType.Name .
        /// </summary>
        private const string DataTypeNameMatchFormat = "@dataTypeName == '{0}'";

        /// <summary>
        /// A format string that allows the TypeMatchExpression to match specific components of the DataSource.DataStreamId.
        /// </summary>
        private const string DataSourceIdMatchFormat = "$.Body.dataSourceId =~ /{0}/";

        /// <summary>
        /// The JSONPath type match expression used to match device content payloads.
        /// *MatchFormat strings will be added to the placeholder.
        /// </summary>
        private const string TypeMatchFormat = "$..[?({0})]";

        /// <summary>
        /// A mapping between the data type format provided by the DataSource and the property name where the value is stored.
        /// </summary>
        private static Dictionary<string, string> _valueTypeMap = new Dictionary<string, string>()
        {
            { MappingConstants.IntegerType, MappingConstants.IntegerValue },
            { MappingConstants.FloatPointType, MappingConstants.FloatPointValue },
            { MappingConstants.StringType, MappingConstants.StringValue },
            { MappingConstants.MapType, MappingConstants.MapValue },
        };

        public DeviceContentTemplateGenerator(params string[] includedDataStreamExpressions)
        {
            if (includedDataStreamExpressions != null && includedDataStreamExpressions.Length > 0)
            {
                foreach (var expression in includedDataStreamExpressions)
                {
                    _includedDataStreamExpressions.Add(expression);
                }
            }
        }

        public override Task<IEnumerable<string>> GetTypeNames(DataSource model, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(model, nameof(model));

            var typeNames = new List<string>();

            if (_includedDataStreamExpressions.Count > 0)
            {
                foreach (var expression in _includedDataStreamExpressions)
                {
                    Match match = Regex.Match(model.DataStreamId, expression, RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        typeNames.Add(GenerateTypeName(model));
                    }
                }
            }
            else
            {
                typeNames.Add(GenerateTypeName(model));
            }

            return Task.FromResult<IEnumerable<string>>(typeNames);
        }

        public override Task<TemplateExpression> GetTypeMatchExpression(string typeName, DataSource model, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(model, nameof(model));

            var expression = new TemplateExpression
            {
                Value = GenerateTypeMatchString(model),
            };

            return Task.FromResult(expression);
        }

        public override Task<TemplateExpression> GetDeviceIdExpression(string typeName, DataSource model, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(model, nameof(model));

            var expression = new TemplateExpression
            {
                Value = $"$.Body.{GoogleFitConstants.DeviceIdentifier}",
            };

            return Task.FromResult(expression);
        }

        public override Task<TemplateExpression> GetTimestampExpression(string typeName, DataSource model, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(model, nameof(model));

            var expression = new TemplateExpression(
                "fromUnixTimestampMs(ceil(multiply(matchedToken.endTimeNanos, `0.000001`)))",
                TemplateExpressionLanguage.JmesPath);

            return Task.FromResult(expression);
        }

        public override Task<TemplateExpression> GetPatientIdExpression(string typeName, DataSource model, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(model, nameof(model));

            var expression = new TemplateExpression
            {
                Value = $"$.Body.{GoogleFitConstants.PatientIdentifier}",
            };

            return Task.FromResult(expression);
        }

        public override Task<IList<CalculatedFunctionValueExpression>> GetValues(string typeName, DataSource model, CancellationToken cancellationToken)
        {
            EnsureArg.IsNotNull(model, nameof(model));

            List<CalculatedFunctionValueExpression> values = new List<CalculatedFunctionValueExpression>();

            for (int i = 0; i < model.DataType.Field.Count; i++)
            {
                DataTypeField field = model.DataType.Field[i];

                values.Add(new CalculatedFunctionValueExpression
                {
                    ValueName = field.Name,
                    ValueExpression = GetValueExpression(field.Name, field.Format, i),
                    Required = !(field.Optional.HasValue ? field.Optional.Value : false),
                });
            }

            return Task.FromResult<IList<CalculatedFunctionValueExpression>>(values);
        }

        private static string GenerateTypeMatchString(DataSource dataSource)
        {
            EnsureArg.IsNotNull(dataSource, nameof(dataSource));

            var stringBuilder = new StringBuilder(string.Format(DataTypeNameMatchFormat, dataSource.DataType.Name));
            var components = GetDataSourceIdComponents(dataSource, false);

            foreach (string component in components)
            {
                stringBuilder.Append(" && ");
                stringBuilder.Append(string.Format(DataSourceIdMatchFormat, component));
            }

            return string.Format(TypeMatchFormat, stringBuilder.ToString());
        }

        private static string GenerateTypeName(DataSource dataSource)
        {
            EnsureArg.IsNotNull(dataSource, nameof(dataSource));

            var components = GetDataSourceIdComponents(dataSource);

            return string.Join(":", components);
        }

        private static TemplateExpression GetValueExpression(string name, string format, int index)
        {
            EnsureArg.IsNotNull(format, nameof(format));

            string fieldName = _valueTypeMap[format];

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new InvalidOperationException($"The format type {format} is not supported.");
            }

            string expression = EnumConverter.GetValueMatchExpression(name, fieldName, index);

            if (!string.IsNullOrWhiteSpace(expression))
            {
                // Value is a Google documented enum value. Add the JMESPath enum converter
                return new TemplateExpression(expression, TemplateExpressionLanguage.JmesPath);
            }

            return new TemplateExpression
            {
                Value = $"matchedToken.value[{index}].{fieldName}",
            };
        }

        private static List<string> GetDataSourceIdComponents(DataSource dataSource, bool includeDataTypeName = true)
        {
            EnsureArg.IsNotNull(dataSource, nameof(dataSource));

            var components = new List<string>();

            if (!string.IsNullOrWhiteSpace(dataSource.Type))
            {
                components.Add(dataSource.Type);
            }

            if (includeDataTypeName && !string.IsNullOrWhiteSpace(dataSource.DataType?.Name))
            {
                components.Add(dataSource.DataType.Name);
            }

            if (!string.IsNullOrWhiteSpace(dataSource.Application?.PackageName))
            {
                components.Add(dataSource.Application.PackageName);
            }

            if (!string.IsNullOrWhiteSpace(dataSource.Device?.Manufacturer))
            {
                components.Add(dataSource.Device.Manufacturer);
            }

            if (!string.IsNullOrWhiteSpace(dataSource.Device?.Model))
            {
                components.Add(dataSource.Device.Model);
            }

            if (!string.IsNullOrWhiteSpace(dataSource.DataStreamName))
            {
                components.Add(dataSource.DataStreamName);
            }

            if (components.Count == 0)
            {
                throw new InvalidOperationException($"A data could not be generated for DataSource: {dataSource}");
            }

            return components;
        }
    }
}
