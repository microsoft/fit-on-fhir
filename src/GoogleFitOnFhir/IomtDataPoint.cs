using Google.Apis.Fitness.v1.Data;
using Newtonsoft.Json;

namespace GoogleFitOnFhir
{
    public class IomtDataPoint : DataPoint
    {

        [JsonProperty("endTimeISO8601")]
        public virtual string EndTimeISO8601 { get; set; }
    }
}
