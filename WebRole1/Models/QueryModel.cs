using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class QueryModel1
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }
        [JsonProperty(PropertyName = "playerId")]
        public string playerId { get; set; }
        [JsonProperty(PropertyName = "hashedId")]
        public string hashedId { get; set; }
    }

        public class QueryModel
    {
        [JsonProperty(PropertyName = "id")]
        public string id { get; set; }
        [JsonProperty(PropertyName = "playerId")]
        public string playerId { get; set; }
        [JsonProperty(PropertyName = "hashedId")]
        public string hashedId { get; set; }

        [JsonProperty(PropertyName = "countryCode")]
        public string countryCode { get; set; }
        [JsonProperty(PropertyName = "nickname")]
        public string nickname { get; set; }
        [JsonProperty(PropertyName = "nicknameLower")]
        public string nicknameLower { get; set; }

        [JsonProperty(PropertyName = "score")]
        public string score { get; set; }
        [JsonProperty(PropertyName = "secondaryScore")]
        public string secondaryScore { get; set; }
        [JsonProperty(PropertyName = "indexScore")]
        public string indexScore { get; set; }

        [JsonProperty(PropertyName = "level")]
        public string level { get; set; }
        [JsonProperty(PropertyName = "lastSaveUnixTime")]
        public string lastSaveUnixTime { get; set; }
        [JsonProperty(PropertyName = "lastLoadUnixTime")]
        public string lastLoadUnixTime { get; set; }
        [JsonProperty(PropertyName = "disconnectedUnixTime")]
        public string disconnectedUnixTime { get; set; }
        [JsonProperty(PropertyName = "facebookId")]
        public string facebookId { get; set; }
        [JsonProperty(PropertyName = "gameCenterId")]
        public string gameCenterId { get; set; }

        [JsonProperty(PropertyName = "chatMessages")]
        public List<chatMessages> chatMessage { get; set; }

        [JsonProperty(PropertyName = "foo")]
        public string foo { get; set; }

        [JsonProperty(PropertyName = "_rid")]
        public string _rid { get; set; }

        [JsonProperty(PropertyName = "_self")]
        public string _self { get; set; }

        [JsonProperty(PropertyName = "_attachments")]
        public string _attachments { get; set; }
        [JsonProperty(PropertyName = "_ts")]
        public string _ts { get; set; }
       
    }
    public class chatMessages
    {

        [JsonProperty(PropertyName = "playerId")]
        public string playerId { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string message { get; set; }

        [JsonProperty(PropertyName = "time")]
        public string time { get; set; }

        [JsonProperty(PropertyName = "notificationType")]
        public string notificationType { get; set; }
    }


}
