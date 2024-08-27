using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static ComicRecompress.Chainner.ChainnerClient;

namespace ComicRecompress.Chainner.Models
{
    public interface BaseEvent
    {
    }


    public class NodeStart : BaseEvent
    {
        [JsonPropertyName("nodeId")]
        public string NodeId { get; set; }
    }
    public class NodeFinish : BaseEvent
    {
        [JsonPropertyName("nodeId")]
        public string NodeId { get; set; }
        [JsonPropertyName("executionTime")]
        public decimal ExecutionTime { get; set; }
    }


    public class NodeProgress : BaseEvent
    {
        [JsonPropertyName("nodeId")]
        public string NodeId { get; set; }
        [JsonPropertyName("progress")]
        public decimal Progress { get; set; }
        [JsonPropertyName("index")]
        public int Index { get; set; }
        [JsonPropertyName("total")]
        public int Total { get; set; }
        [JsonPropertyName("eta")]
        public decimal Eta { get; set; }
    }


    public class RunRequest
    {
        public List<BackendJsonNode> data { get; set; }
        public JsonNode options { get; set; }
        public bool sendBroadcastData { get; set; }
    }

    public class Response
    {
        public string type { get; set; }
        public string message { get; set; }
        public bool ready { get; set; }
        public Worker worker { get; set; }
    }

}
