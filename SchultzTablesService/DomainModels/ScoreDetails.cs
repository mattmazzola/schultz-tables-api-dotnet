﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SchultzTablesService.Documents;

namespace SchultzTablesService.DomainModels
{
    public class ScoreDetails
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }
        [JsonProperty("sequence")]
        public IList<Answer> Sequence { get; set; }
        [JsonProperty("tableLayout")]
        public TableLayout TableLayout { get; set; }
        [JsonProperty("tableType")]
        public TableType TableType { get; set; }
    }
}
