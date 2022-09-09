// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Web;
using Newtonsoft.Json;

namespace Microsoft.Health.FitOnFhir.Common.Serialization
{
    public class UrlSafeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader?.Value is string stringValue)
            {
                return HttpUtility.UrlDecode(stringValue);
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (writer != null && value is string stringValue)
            {
                writer.WriteValue(HttpUtility.UrlEncode(stringValue));
            }
        }
    }
}
