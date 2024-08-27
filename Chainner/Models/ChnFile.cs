using ComicRecompress.Chainner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace ComicRecompress.Chainner.Models
{
    
    public class Content
    {
        public List<ChnNode> nodes { get; set; }
        public List<ChnEdge> edges { get; set; }
        public Viewport viewport { get; set; }
    }

    public class Data
    {
        public string schemaId { get; set; }
        public Dictionary<int, object> inputData { get; set; }
        public string id { get; set; }
        public Dictionary<int, int> inputHeight { get; set; }
        public int? nodeWidth { get; set; }
    }

    public class ChnEdge
    {
        public string id { get; set; }
        public string sourceHandle { get; set; }
        public string targetHandle { get; set; }
        public string source { get; set; }
        public string target { get; set; }
        public string type { get; set; }
        public bool animated { get; set; }
        public Data data { get; set; }
        public bool? selected { get; set; }
    }



    public class ChnNode
    {
        public Data data { get; set; }
        public string id { get; set; }
        public Position position { get; set; }
        public string type { get; set; }
        public bool selected { get; set; }
        public int height { get; set; }
        public int width { get; set; }
    }

    public class Position
    {
        public double x { get; set; }
        public double y { get; set; }
    }

    public class ChnFile
    {
        public string version { get; set; }
        public Content content { get; set; }
        public DateTime timestamp { get; set; }
        public string checksum { get; set; }
        public int migration { get; set; }
    }

    public class Viewport
    {
        public double x { get; set; }
        public double y { get; set; }
        public double zoom { get; set; }
    }


}
