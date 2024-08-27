using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComicRecompress.Chainner.Models
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Category
    {
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string icon { get; set; }
        public string color { get; set; }
        public string installHint { get; set; }
        public List<Group> groups { get; set; }
    }

    public class Condition
    {
        public string kind { get; set; }
        public int @enum { get; set; }
        public List<string> values { get; set; }
        public Condition condition { get; set; }
    }

    public class Conversion
    {
        public string type { get; set; }
        public string convert { get; set; }
    }

    public class Fields
    {
        public string length { get; set; }
    }

    public class Group
    {
        public string label { get; set; }
        public object startAt { get; set; }
        public string id { get; set; }
        public string category { get; set; }
        public string name { get; set; }
        public List<string> order { get; set; }
    }

    public class Input
    {
        public int id { get; set; }
        public object type { get; set; }
        public List<Conversion> conversions { get; set; }
        public string adapt { get; set; }
        public string typeDefinitions { get; set; }
        public string kind { get; set; }
        public string label { get; set; }
        public bool optional { get; set; }
        public bool hasHandle { get; set; }
        public string description { get; set; }
        public bool hint { get; set; }
        public bool suggest { get; set; }
        public object fused { get; set; }
        public List<string> filetypes { get; set; }
        public string fileKind { get; set; }
        public bool primaryInput { get; set; }
        public string labelStyle { get; set; }
        public int? minLength { get; set; }
        public object maxLength { get; set; }
        public object placeholder { get; set; }
        public bool? multiline { get; set; }
        public object def { get; set; }
        public bool? allowEmptyString { get; set; }
        public string invalidPattern { get; set; }
        public List<Option> options { get; set; }
        public string preferredStyle { get; set; }
        public List<Group> groups { get; set; }
        public double? min { get; set; }
        public double? max { get; set; }
        public object noteExpression { get; set; }
        public int? precision { get; set; }
        public double? controlsStep { get; set; }
        public string unit { get; set; }
        public bool? hideTrailingZeros { get; set; }
        public List<string> ends { get; set; }
        public double? sliderStep { get; set; }
        public List<string> gradient { get; set; }
        public string scale { get; set; }
        public List<int> channels { get; set; }
    }

    public class IteratorInput
    {
        public int id { get; set; }
        public List<int> inputs { get; set; }
        public SequenceType sequenceType { get; set; }
    }

    public class IteratorOutput
    {
        public int id { get; set; }
        public List<int> outputs { get; set; }
        public SequenceType sequenceType { get; set; }
    }

    public class KeyInfo
    {
        public string kind { get; set; }
        public int inputId { get; set; }
        public string expression { get; set; }
    }

    public class Node
    {
        public string schemaId { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string nodeGroup { get; set; }
        public List<Input> inputs { get; set; }
        public List<Output> outputs { get; set; }
        public List<object> groupLayout { get; set; }
        public List<IteratorInput> iteratorInputs { get; set; }
        public List<IteratorOutput> iteratorOutputs { get; set; }
        public KeyInfo keyInfo { get; set; }
        public List<object> suggestions { get; set; }
        public string description { get; set; }
        public List<string> seeAlso { get; set; }
        public string icon { get; set; }
        public string kind { get; set; }
        public bool hasSideEffects { get; set; }
        public bool deprecated { get; set; }
        public List<object> features { get; set; }
    }

    public class Option
    {
        public string option { get; set; }
        public object value { get; set; }
        public object type { get; set; }
        public Condition condition { get; set; }
        public string icon { get; set; }
    }

    public class Output
    {
        public int id { get; set; }
        public object type { get; set; }
        public string label { get; set; }
        public string neverReason { get; set; }
        public string kind { get; set; }
        public bool hasHandle { get; set; }
        public int? passthroughOf { get; set; }
        public string description { get; set; }
        public bool suggest { get; set; }
    }

    public class NodesSchema
    {
        public List<Node> nodes { get; set; }
        public List<Category> categories { get; set; }
        public List<object> categoriesMissingNodes { get; set; }
    }


    public class SequenceType
    {
        public string type { get; set; }
        public string name { get; set; }
        public Fields fields { get; set; }
    }



    public class BackendJsonNode
    {
        public string id { get; set; }
        public string schemaId { get; set; }
        public List<BackendJsonInput> inputs { get; set; }
        public string nodeType { get; set; }
    }

    public class BackendJsonInput
    {
        public string type { get; set; }
        public string id { get; set; }
        public int index { get; set; }
        public object value { get; set; }
    }

    public class ParsedSourceHandle
    {
        public string NodeId { get; set; }
        public int Id { get; set; }

        public ParsedSourceHandle(string str)
        {
            int idx = str.LastIndexOf("-");
            NodeId = str.Substring(0, idx);
            Id = int.Parse(str.Substring(idx + 1));
        }
    }
}
