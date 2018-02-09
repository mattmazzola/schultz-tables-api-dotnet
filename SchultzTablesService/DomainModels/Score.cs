using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SchultzTablesService.DomainModels
{
    public class Score
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; }
        [JsonProperty("durationMilliseconds")]
        public double DurationMilliseconds { get; set; }
    }
}
