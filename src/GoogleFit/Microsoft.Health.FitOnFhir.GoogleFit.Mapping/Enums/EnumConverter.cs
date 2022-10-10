// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Extensions;

namespace Microsoft.Health.FitOnFhir.GoogleFit.Mapping.Enums
{
    public static class EnumConverter
    {
        public static string GetValueMatchExpression(string typeName, string fieldName, int fieldIndex)
        {
            string enumName = typeName.UnderscoreToCamelCase();
            Type enumType = Type.GetType($"{typeof(EnumConverter).Namespace}.{enumName}");

            if (enumType != null)
            {
                Array values = Enum.GetValues(enumType);

                if (values.Length > 0)
                {
                    var builder = new StringBuilder($"matchedToken.value[{fieldIndex}].{fieldName} | [");

                    for (int i = 0; i < values.Length; i++)
                    {
                        builder.Append($"{{\"v\":@,\"n\":`{i}`,\"s\":'{values.GetValue(i)}'}}");

                        if (i < values.Length - 1)
                        {
                            builder.Append(',');
                        }
                    }

                    builder.Append("][?v == n].s | @[0]");
                    return builder.ToString();
                }
            }

            return default;
        }
    }
}
