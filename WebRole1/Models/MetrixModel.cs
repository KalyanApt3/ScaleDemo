using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1.Models
{
    public class MetrixModel
    {
        
        [JsonProperty(PropertyName="id")]
        public string id { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string type { get; set; }
        [JsonProperty(PropertyName = "totalDocumentsCreated")]
        public long totalDocumentsCreated { get; set; }
        [JsonProperty(PropertyName = "documentsCreatedPerSecond")]
        public long documentsCreatedPerSecond { get; set; }
        [JsonProperty(PropertyName = "requestUnitsPerSecond")]
        public long requestUnitsPerSecond { get; set; }
        [JsonProperty(PropertyName = "requestUnitsPerMonth")]
        public long requestUnitsPerMonth { get; set; }
        [JsonProperty(PropertyName = "documentsCreatedInLastSecond")]
        public long documentsCreatedInLastSecond { get; set; }
        [JsonProperty(PropertyName = "requestUnitsInLastSecond")]
        public long requestUnitsInLastSecond { get; set; }
        [JsonProperty(PropertyName = "requestUnitsPerMonthBasedOnLastSecond")]
        public long requestUnitsPerMonthBasedOnLastSecond { get; set; }

    }
}