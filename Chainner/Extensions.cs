using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComicRecompress.Chainner.Models;

namespace ComicRecompress.Chainner
{
    internal static class Extensions
    {
        private static BackendJsonInput ConvertHandle(Dictionary<string, Node> nodeSchemaMap, ParsedSourceHandle handle)
        {
            if (!nodeSchemaMap.TryGetValue(handle.NodeId, out var schema))
            {
                throw new Exception($"Invalid handle: The node id {handle.NodeId} is not valid");
            }

            var index = schema.outputs.FindIndex(inOut => inOut.id == handle.Id);
            if (index == -1)
            {
                throw new Exception($"Invalid handle: There is no output with id {handle.Id} in {schema.name}");
            }

            return new BackendJsonInput { type = "edge", id = handle.NodeId, index = index };
        }
        private static List<BackendJsonInput> MapInputValues(Node schema, Func<int, BackendJsonInput> mapFunc)
        {
            return schema.inputs.Select(input => mapFunc(input.id)).ToList();
        }


        public static List<BackendJsonNode> ToBackendJson(this ChnFile file, List<Node> schemaNodes)
        {
            var nodeSchemaMap = file.content.nodes.ToDictionary(n => n.id, n => schemaNodes.First(b => b.schemaId == n.data.schemaId));
            var inputHandles = new Dictionary<string, Dictionary<int, BackendJsonInput>>();

            foreach (var edge in file.content.edges)
            {
                if (edge.sourceHandle == null || edge.targetHandle == null) continue;

                var sourceH = new ParsedSourceHandle(edge.sourceHandle);
                var targetH = new ParsedSourceHandle(edge.targetHandle);

                if (!inputHandles.ContainsKey(targetH.NodeId))
                    inputHandles[targetH.NodeId] = new Dictionary<int, BackendJsonInput>();
                inputHandles[targetH.NodeId][targetH.Id] = ConvertHandle(nodeSchemaMap, sourceH);
            }

            var result = new List<BackendJsonNode>();

            foreach (var node in file.content.nodes)
            {
                var schemaId = node.data.schemaId;
                var schema = schemaNodes.First(b => b.schemaId == schemaId);

                if (node.type == null)
                {
                    throw new Exception($"Expected all nodes to have a node type, but {schema.name} (id: {schemaId}) node did not.");
                }

                result.Add(new BackendJsonNode
                {
                    id = node.id,
                    schemaId = schemaId,
                    inputs = MapInputValues(schema, (inputId) =>
                    {
                        return inputHandles.TryGetValue(node.id, out var handles) &&
                            handles.TryGetValue(inputId, out var handle)
                                ? handle
                                : new BackendJsonInput
                                {
                                    type = "value",
                                    value = node.data.inputData.ContainsKey(inputId)
                                        ? node.data.inputData[inputId]
                                        : null
                                };
                    })


                        ,
                    nodeType = node.type
                });
            }

            return result;
        }
    }
}
