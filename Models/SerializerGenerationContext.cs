using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ComicRecompress.Chainner.Models;

namespace ComicRecompress.Models;

[JsonSerializable(typeof(ChnFile))]
[JsonSerializable(typeof(Data))]
[JsonSerializable(typeof(ChnEdge))]
[JsonSerializable(typeof(ChnNode))]
[JsonSerializable(typeof(Position))]
[JsonSerializable(typeof(Viewport))]
[JsonSerializable(typeof(NodeStart))]
[JsonSerializable(typeof(NodeProgress))]
[JsonSerializable(typeof(NodeFinish))]
[JsonSerializable(typeof(Category))]
[JsonSerializable(typeof(Condition))]
[JsonSerializable(typeof(Conversion))]
[JsonSerializable(typeof(Fields))]
[JsonSerializable(typeof(Group))]
[JsonSerializable(typeof(Input))]
[JsonSerializable(typeof(IteratorInput))]
[JsonSerializable(typeof(IteratorOutput))]
[JsonSerializable(typeof(KeyInfo))]
[JsonSerializable(typeof(Node))]
[JsonSerializable(typeof(Option))]
[JsonSerializable(typeof(Output))]
[JsonSerializable(typeof(NodesSchema))]
[JsonSerializable(typeof(SequenceType))]
[JsonSerializable(typeof(BackendJsonNode))]
[JsonSerializable(typeof(BackendJsonInput))]
[JsonSerializable(typeof(RunRequest))]
[JsonSerializable(typeof(Response))]
[JsonSerializable(typeof(ProcessState))]
[JsonSerializable(typeof(ExecutionState))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonProperty))]
internal partial class SerializerGenerationContext : JsonSerializerContext
{
    public static JsonSerializerOptions Options => new JsonSerializerOptions
    {
        TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault ? new DefaultJsonTypeInfoResolver() : Default
    };
}